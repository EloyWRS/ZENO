using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZENO_API_II.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenVersionToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Threads_Users_UserId",
                table: "Threads");

            migrationBuilder.DropIndex(
                name: "IX_Threads_UserId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Threads");

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Threads",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Threads_UserId",
                table: "Threads",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Threads_Users_UserId",
                table: "Threads",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
