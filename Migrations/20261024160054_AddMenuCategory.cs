using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManTingEats.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Category",
                table: "MenuItems",
                newName: "CategoryId");

            migrationBuilder.CreateTable(
                name: "MenuCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // 分類改為資料表（v17）：先建立原列舉的三個分類，再把品項原本存的列舉值（Food=0/Drink=1/Other=2）
            // 換成對應分類的 Id，之後才加外鍵，既有品項不會遺失分類
            migrationBuilder.Sql("""
                INSERT INTO MenuCategories (Id, Name, SortOrder, IsActive) VALUES
                    (1, '吃', 0, 1),
                    (2, '喝', 1, 1),
                    (3, '其他', 2, 1);
                """);

            migrationBuilder.Sql("""
                UPDATE MenuItems
                SET CategoryId = CASE CategoryId
                    WHEN 0 THEN 1
                    WHEN 1 THEN 2
                    ELSE 3
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_Name",
                table: "MenuCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_MenuCategories_CategoryId",
                table: "MenuItems",
                column: "CategoryId",
                principalTable: "MenuCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_MenuCategories_CategoryId",
                table: "MenuItems");

            // 還原為列舉值：只能對應回三個初始分類，之後新增的分類一律歸為「其他」
            migrationBuilder.Sql("""
                UPDATE MenuItems
                SET CategoryId = CASE CategoryId
                    WHEN 1 THEN 0
                    WHEN 2 THEN 1
                    ELSE 2
                END;
                """);

            migrationBuilder.DropTable(
                name: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_CategoryId",
                table: "MenuItems");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "MenuItems",
                newName: "Category");
        }
    }
}
