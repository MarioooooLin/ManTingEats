using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using ManTingEats.Controllers;
using ManTingEats.Models;
using ManTingEats.Models.Commands;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Enums;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using static ManTingEats.Tests.AccountTestHost;

namespace ManTingEats.Tests;

public sealed class AccountPermissionTests : IDisposable
{
    private readonly AccountTestHost _host = new();

    public void Dispose() => _host.Dispose();

    // ── 權限設定（v7 第 2 節） ──────────────────────────────────

    [Theory]
    [InlineData(typeof(MenuController))]
    [InlineData(typeof(ExpenseController))]
    [InlineData(typeof(ReportController))]
    [InlineData(typeof(EmployeeController))]
    public void ManagerOnlyControllers_RequireManagerRole(Type controller)
    {
        var attribute = controller.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(AppRoles.Manager, attribute.Roles);
    }

    [Theory]
    [InlineData(typeof(OrderController))]
    [InlineData(typeof(ReservationController))]
    public void StaffControllers_RequireLoginOnly(Type controller)
    {
        var attribute = controller.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(attribute);
        Assert.Null(attribute.Roles);
    }

    [Fact]
    public void ManagerRoleConstant_MatchesRoleWrittenAtLogin()
    {
        // 登入時寫入 Cookie 的角色是 EmployeeRole.ToString()，必須與 [Authorize(Roles)] 使用的字串一致
        var principal = EmployeeSession.CreatePrincipal(new Employee { Username = "boss", PasswordHash = "x", Role = EmployeeRole.Manager });

        Assert.True(principal.IsInRole(AppRoles.Manager));
    }

    // ── 登入狀態驗證（安全戳記） ─────────────────────────────────

    [Fact]
    public async Task Session_ActiveWithCurrentStamp_IsValid()
    {
        using var db = _host.CreateDbContext();

        Assert.True(await EmployeeSession.IsValidAsync(EmployeeSession.CreatePrincipal(_host.Load(StaffId)), db));
    }

    [Fact]
    public async Task Session_AfterPasswordReset_IsInvalid()
    {
        var oldPrincipal = EmployeeSession.CreatePrincipal(_host.Load(StaffId));

        await _host.CreateEmployeeController().ResetPassword(StaffId, new ResetPasswordCommand { NewPassword = "new-pass-123", ConfirmNewPassword = "new-pass-123" });

        using var db = _host.CreateDbContext();
        Assert.False(await EmployeeSession.IsValidAsync(oldPrincipal, db));
    }

    [Fact]
    public async Task Session_AfterDeactivation_IsInvalid()
    {
        var oldPrincipal = EmployeeSession.CreatePrincipal(_host.Load(StaffId));

        await _host.CreateEmployeeController().ToggleActive(StaffId);

        using var db = _host.CreateDbContext();
        Assert.False(await EmployeeSession.IsValidAsync(oldPrincipal, db));
    }

    [Fact]
    public async Task Session_CookieIssuedBeforeV7WithoutStamp_IsInvalid()
    {
        // 部署前發出的 Cookie 沒有安全戳記，需重新登入一次
        var legacy = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ManagerId.ToString())], "Test"));
        using var db = _host.CreateDbContext();

        Assert.False(await EmployeeSession.IsValidAsync(legacy, db));
    }

    // ── 新增員工 ──────────────────────────────────────────────

    [Fact]
    public async Task Create_AddsActiveStaffWithHashedPassword()
    {
        var result = await _host.CreateEmployeeController().Create(
            new CreateEmployeeCommand { Username = "  newbie  ", Password = "newbie-pass", ConfirmPassword = "newbie-pass" });

        Assert.IsType<RedirectToActionResult>(result);
        using var db = _host.CreateDbContext();
        var created = db.Employees.Single(e => e.Username == "newbie");   // 前後空白已去除
        Assert.Equal(EmployeeRole.Staff, created.Role);
        Assert.True(created.IsActive);
        Assert.NotEqual("newbie-pass", created.PasswordHash);
        Assert.True(_host.PasswordMatches(created.Id, "newbie-pass"));
    }

    [Fact]
    public async Task Create_DuplicateUsernameIgnoringCase_Rejected()
    {
        var controller = _host.CreateEmployeeController();

        var result = await controller.Create(new CreateEmployeeCommand { Username = "STAFF1", Password = "whatever-1", ConfirmPassword = "whatever-1" });

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        using var db = _host.CreateDbContext();
        Assert.Equal(2, db.Employees.Count());
    }

    [Theory]
    [InlineData("ab", "long-enough", "long-enough")]      // 帳號太短
    [InlineData("valid", "short", "short")]               // 密碼少於 8 碼
    [InlineData("valid", "long-enough", "different!")]    // 兩次密碼不一致
    public void CreateCommand_InvalidInput_FailsValidation(string username, string password, string confirm)
    {
        var command = new CreateEmployeeCommand { Username = username, Password = password, ConfirmPassword = confirm };

        Assert.False(Validator.TryValidateObject(command, new ValidationContext(command), [], validateAllProperties: true));
    }

    // ── 重設密碼與停用（只能操作員工） ────────────────────────────

    [Fact]
    public async Task ResetPassword_Staff_ChangesPasswordAndStamp()
    {
        var oldStamp = _host.Load(StaffId).SecurityStamp;

        await _host.CreateEmployeeController().ResetPassword(StaffId, new ResetPasswordCommand { NewPassword = "new-pass-123", ConfirmNewPassword = "new-pass-123" });

        Assert.True(_host.PasswordMatches(StaffId, "new-pass-123"));
        Assert.NotEqual(oldStamp, _host.Load(StaffId).SecurityStamp);
    }

    [Fact]
    public async Task ResetPassword_Manager_Rejected()
    {
        var controller = _host.CreateEmployeeController();

        await controller.ResetPassword(ManagerId, new ResetPasswordCommand { NewPassword = "new-pass-123", ConfirmNewPassword = "new-pass-123" });

        Assert.True(_host.PasswordMatches(ManagerId, InitialPassword));
        Assert.NotNull(controller.TempData["Error"]);
    }

    [Fact]
    public async Task ToggleActive_Staff_DeactivatesThenReactivates()
    {
        await _host.CreateEmployeeController().ToggleActive(StaffId);
        Assert.False(_host.Load(StaffId).IsActive);

        await _host.CreateEmployeeController().ToggleActive(StaffId);
        Assert.True(_host.Load(StaffId).IsActive);
    }

    [Fact]
    public async Task ToggleActive_Manager_Rejected()
    {
        var controller = _host.CreateEmployeeController();

        await controller.ToggleActive(ManagerId);

        Assert.True(_host.Load(ManagerId).IsActive);
        Assert.NotNull(controller.TempData["Error"]);
    }

    // ── 店長變更自己的密碼 ──────────────────────────────────────

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Rejected()
    {
        var controller = _host.CreateEmployeeController();

        var result = await controller.ChangePassword(new ChangePasswordCommand
        {
            CurrentPassword = "wrong-pass", NewPassword = "new-pass-123", ConfirmNewPassword = "new-pass-123"
        });

        Assert.IsType<ViewResult>(result);
        Assert.True(_host.PasswordMatches(ManagerId, InitialPassword));
        Assert.Null(_host.Auth.SignedIn);
    }

    [Fact]
    public async Task ChangePassword_Correct_UpdatesPasswordAndKeepsCurrentDeviceSignedIn()
    {
        var oldStamp = _host.Load(ManagerId).SecurityStamp;

        await _host.CreateEmployeeController().ChangePassword(new ChangePasswordCommand
        {
            CurrentPassword = InitialPassword, NewPassword = "new-pass-123", ConfirmNewPassword = "new-pass-123"
        });

        var manager = _host.Load(ManagerId);
        Assert.True(_host.PasswordMatches(ManagerId, "new-pass-123"));
        Assert.NotEqual(oldStamp, manager.SecurityStamp);
        // 目前裝置以新戳記重新簽發 Cookie，不會被自己的改密碼登出
        Assert.Equal(manager.SecurityStamp, _host.Auth.SignedIn?.FindFirstValue(EmployeeSession.SecurityStampClaim));
    }

    // ── 登入 ────────────────────────────────────────────────

    [Fact]
    public async Task Login_ActiveStaff_SignsInWithRoleAndStamp()
    {
        await _host.CreateAccountController().Login(new LoginViewModel { Username = "staff1", Password = InitialPassword });

        var principal = _host.Auth.SignedIn;
        Assert.NotNull(principal);
        Assert.False(principal.IsInRole(AppRoles.Manager));
        Assert.Equal(_host.Load(StaffId).SecurityStamp, principal.FindFirstValue(EmployeeSession.SecurityStampClaim));
    }

    [Fact]
    public async Task Login_DeactivatedWithCorrectPassword_ShowsDisabledMessage()
    {
        await _host.CreateEmployeeController().ToggleActive(StaffId);
        var controller = _host.CreateAccountController();

        await controller.Login(new LoginViewModel { Username = "staff1", Password = InitialPassword });

        Assert.Null(_host.Auth.SignedIn);
        Assert.Contains("此帳號已停用", controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Login_DeactivatedWithWrongPassword_DoesNotRevealStatus()
    {
        await _host.CreateEmployeeController().ToggleActive(StaffId);
        var controller = _host.CreateAccountController();

        await controller.Login(new LoginViewModel { Username = "staff1", Password = "wrong-pass" });

        Assert.Null(_host.Auth.SignedIn);
        Assert.Equal("帳號或密碼錯誤。", controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }
}
