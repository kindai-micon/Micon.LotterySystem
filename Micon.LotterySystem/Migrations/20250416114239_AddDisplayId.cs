﻿using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Micon.LotterySystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DisplayId",
                table: "LotterySlots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DisplayId",
                table: "LotteryGroups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayId",
                table: "LotterySlots");

            migrationBuilder.DropColumn(
                name: "DisplayId",
                table: "LotteryGroups");
        }
    }
}
