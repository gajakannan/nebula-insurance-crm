using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0004_AddTaskAndUserProfileIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_DisplayName",
                table: "UserProfiles",
                column: "DisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_CreatedByUserId_AssignedToUserId",
                table: "Tasks",
                columns: new[] { "CreatedByUserId", "AssignedToUserId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_DisplayName",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_CreatedByUserId_AssignedToUserId",
                table: "Tasks");
        }
    }
}
