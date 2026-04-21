using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBrokerEmailPhone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Brokers",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "Brokers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "Brokers");
        }
    }
}
