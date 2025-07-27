using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZENO_API_II.Migrations
{
    /// <inheritdoc />
    public partial class AddOpenAIThreadIdToChatThread : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assistants_Users_UserId",
                table: "Assistants");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Assistants",
                newName: "UserLocalId");

            migrationBuilder.RenameIndex(
                name: "IX_Assistants_UserId",
                table: "Assistants",
                newName: "IX_Assistants_UserLocalId");

            migrationBuilder.AddColumn<string>(
                name: "OpenAI_ThreadId",
                table: "Threads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenAI_Id",
                table: "Assistants",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Assistants_Users_UserLocalId",
                table: "Assistants",
                column: "UserLocalId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assistants_Users_UserLocalId",
                table: "Assistants");

            migrationBuilder.DropColumn(
                name: "OpenAI_ThreadId",
                table: "Threads");

            migrationBuilder.DropColumn(
                name: "OpenAI_Id",
                table: "Assistants");

            migrationBuilder.RenameColumn(
                name: "UserLocalId",
                table: "Assistants",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Assistants_UserLocalId",
                table: "Assistants",
                newName: "IX_Assistants_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assistants_Users_UserId",
                table: "Assistants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
