using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManTingEats.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderDailyNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyNumber",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // 回填既有訂單的當日流水號（以台灣時區 UTC+8 為每日邊界，台灣不實施日光節約時間，固定位移即可）
            migrationBuilder.Sql(@"
                UPDATE Orders o
                JOIN (
                    SELECT Id, ROW_NUMBER() OVER (
                        PARTITION BY DATE(CreatedAt + INTERVAL 8 HOUR)
                        ORDER BY CreatedAt
                    ) AS rn
                    FROM Orders
                ) ranked ON o.Id = ranked.Id
                SET o.DailyNumber = ranked.rn;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyNumber",
                table: "Orders");
        }
    }
}
