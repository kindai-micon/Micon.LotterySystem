using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micon.LotterySystem.Migrations
{
    /// <inheritdoc />
    public partial class ticketinfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TicketInfoId",
                table: "LotteryGroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "TicketInfo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Warning = table.Column<string>(type: "text", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketInfo", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LotteryGroups_TicketInfoId",
                table: "LotteryGroups",
                column: "TicketInfoId");

            migrationBuilder.AddForeignKey(
                name: "FK_LotteryGroups_TicketInfo_TicketInfoId",
                table: "LotteryGroups",
                column: "TicketInfoId",
                principalTable: "TicketInfo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LotteryGroups_TicketInfo_TicketInfoId",
                table: "LotteryGroups");

            migrationBuilder.DropTable(
                name: "TicketInfo");

            migrationBuilder.DropIndex(
                name: "IX_LotteryGroups_TicketInfoId",
                table: "LotteryGroups");

            migrationBuilder.DropColumn(
                name: "TicketInfoId",
                table: "LotteryGroups");
        }
    }
}
