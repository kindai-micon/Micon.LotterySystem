using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micon.LotterySystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketInfoDisplayFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Warning",
                table: "TicketInfo",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "FooterText",
                table: "TicketInfo",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TicketLabel",
                table: "TicketInfo",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterText",
                table: "TicketInfo");

            migrationBuilder.DropColumn(
                name: "TicketLabel",
                table: "TicketInfo");

            migrationBuilder.AlterColumn<string>(
                name: "Warning",
                table: "TicketInfo",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
