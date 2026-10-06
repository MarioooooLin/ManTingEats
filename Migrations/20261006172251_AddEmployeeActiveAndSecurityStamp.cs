using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManTingEats.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeActiveAndSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Employees",
                type: "tinyint(1)",
                nullable: false,
                // 既有帳號（正式環境的店長）一律視為啟用；新帳號由 Employee.IsActive 的 C# 預設值 true 決定
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "Employees",
                type: "varchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            // 既有帳號補上隨機安全戳記；部署前發出的登入 Cookie 沒有戳記，會被要求重新登入一次
            migrationBuilder.Sql("UPDATE `Employees` SET `SecurityStamp` = REPLACE(UUID(), '-', '') WHERE `SecurityStamp` = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "Employees");
        }
    }
}
