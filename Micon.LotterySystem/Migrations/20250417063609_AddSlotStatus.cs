using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micon.LotterySystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "LotterySlots",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "LotterySlots");
        }
    }
}
