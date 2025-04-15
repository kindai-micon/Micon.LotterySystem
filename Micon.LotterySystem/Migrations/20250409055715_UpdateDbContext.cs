using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micon.LotterySystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LotteryGroups_TicketInfoId",
                table: "LotteryGroups");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "LotteryGroups");

            migrationBuilder.CreateIndex(
                name: "IX_LotteryGroups_TicketInfoId",
                table: "LotteryGroups",
                column: "TicketInfoId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LotteryGroups_TicketInfoId",
                table: "LotteryGroups");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "LotteryGroups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LotteryGroups_TicketInfoId",
                table: "LotteryGroups",
                column: "TicketInfoId");
        }
    }
}
