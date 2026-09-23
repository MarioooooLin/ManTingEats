using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManTingEats.Migrations
{
    /// <inheritdoc />
    public partial class ChangeMenuItemCategoryToEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 分類改為固定 Enum（Food=0/Drink=1/Other=2）前，先將既有文字資料轉換為對應數值，避免既有菜單品項遺失分類
            migrationBuilder.Sql("""
                UPDATE MenuItems
                SET Category = CASE Category
                    WHEN '吃' THEN '0'
                    WHEN '喝' THEN '1'
                    ELSE '2'
                END;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "Category",
                table: "MenuItems",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Category",
                table: "MenuItems",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.Sql("""
                UPDATE MenuItems
                SET Category = CASE Category
                    WHEN '0' THEN '吃'
                    WHEN '1' THEN '喝'
                    ELSE '其他'
                END;
                """);
        }
    }
}
