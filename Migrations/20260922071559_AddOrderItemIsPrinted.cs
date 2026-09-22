using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManTingEats.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemIsPrinted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrinted",
                table: "OrderItems",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrinted",
                table: "OrderItems");
        }
    }
}
