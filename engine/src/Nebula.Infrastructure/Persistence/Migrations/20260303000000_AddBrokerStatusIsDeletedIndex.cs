using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokerStatusIsDeletedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Brokers_Status_IsDeleted",
                table: "Brokers",
                columns: new[] { "Status", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Brokers_Status_IsDeleted",
                table: "Brokers");
        }
    }
}
