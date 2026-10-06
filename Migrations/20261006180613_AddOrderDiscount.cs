using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManTingEats.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDiscount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Orders",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DiscountNote",
                table: "Orders",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "DiscountReason",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountedByEmployeeId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiscountedByEmployeeId",
                table: "Orders",
                column: "DiscountedByEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Employees_DiscountedByEmployeeId",
                table: "Orders",
                column: "DiscountedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Employees_DiscountedByEmployeeId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DiscountedByEmployeeId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountNote",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountedByEmployeeId",
                table: "Orders");
        }
    }
}
