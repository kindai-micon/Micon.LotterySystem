using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micon.LotterySystem.Migrations
{
    /// <inheritdoc />
    public partial class TicketUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Number",
                table: "Tickets",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "DisplayId",
                table: "Tickets",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayId",
                table: "Tickets");

            migrationBuilder.AlterColumn<string>(
                name: "Number",
                table: "Tickets",
                type: "text",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
