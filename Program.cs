using System.Text;
using ManTingEats.Data;
using ManTingEats.Models.Entities;
using ManTingEats.Models.Options;
using ManTingEats.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// 印表機字元模式為 Big5（代碼頁 950），.NET 預設不內建此編碼，需先註冊提供者
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        // ASP.NET Core 內建的 Model Binding 失敗訊息（如輸入非數字、日期格式錯誤）預設為英文，這裡覆寫為中文
        var provider = options.ModelBindingMessageProvider;
        provider.SetValueMustNotBeNullAccessor(fieldName => $"{fieldName} 為必填欄位。");
        provider.SetValueIsInvalidAccessor(value => $"「{value}」為無效的值。");
        provider.SetValueMustBeANumberAccessor(fieldName => $"{fieldName} 必須為數字。");
        provider.SetAttemptedValueIsInvalidAccessor((value, fieldName) => $"「{value}」不是有效的 {fieldName} 值。");
        provider.SetMissingKeyOrValueAccessor(() => "此欄位為必填。");
        provider.SetMissingRequestBodyRequiredValueAccessor(() => "必須提供要求內容。");
        provider.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"「{value}」不是有效的值。");
        provider.SetNonPropertyUnknownValueIsInvalidAccessor(() => "提供的值無效。");
        provider.SetNonPropertyValueMustBeANumberAccessor(() => "欄位必須為數字。");
        provider.SetUnknownValueIsInvalidAccessor(fieldName => $"{fieldName} 提供的值無效。");
    });

// 容器化部署時 Key 需持久化於掛載的 volume，否則容器重啟會導致所有登入 session 失效
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "keys")));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("找不到 ConnectionStrings:DefaultConnection 設定，請確認 appsettings.Development.json 或環境變數。");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.Create(new Version(8, 0, 0), Pomelo.EntityFrameworkCore.MySql.Infrastructure.ServerType.MySql)));

builder.Services.AddScoped<IPasswordHasher<Employee>, PasswordHasher<Employee>>();
builder.Services.AddMemoryCache();

builder.Services.Configure<PrinterOptions>(builder.Configuration.GetSection(PrinterOptions.SectionName));
builder.Services.AddSingleton<IReceiptPrinterService, LanReceiptPrinterService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// 首次啟動時若無任何員工帳號，種一組管理者帳號（帳密來自設定檔，不寫死於程式碼）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Employee>>();
    await DbSeeder.SeedManagerAsync(db, hasher, app.Configuration);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 反向代理（Caddy）終止 TLS 後以 HTTP 轉發，需信任其 X-Forwarded-* 標頭才能正確判斷原始 scheme
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// Caddy 與 web 在同一個 Docker Compose 內部網路，來源 IP 為動態分配，故信任所有內部代理
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
