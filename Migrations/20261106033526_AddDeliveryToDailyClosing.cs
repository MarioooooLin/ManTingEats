using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManTingEats.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryToDailyClosing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FoodpandaOrderCount",
                table: "DailyClosings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "FoodpandaPayout",
                table: "DailyClosings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FoodpandaSales",
                table: "DailyClosings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UberEatsOrderCount",
                table: "DailyClosings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "UberEatsPayout",
                table: "DailyClosings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UberEatsSales",
                table: "DailyClosings",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FoodpandaOrderCount",
                table: "DailyClosings");

            migrationBuilder.DropColumn(
                name: "FoodpandaPayout",
                table: "DailyClosings");

            migrationBuilder.DropColumn(
                name: "FoodpandaSales",
                table: "DailyClosings");

            migrationBuilder.DropColumn(
                name: "UberEatsOrderCount",
                table: "DailyClosings");

            migrationBuilder.DropColumn(
                name: "UberEatsPayout",
                table: "DailyClosings");

            migrationBuilder.DropColumn(
                name: "UberEatsSales",
                table: "DailyClosings");
        }
    }
}
