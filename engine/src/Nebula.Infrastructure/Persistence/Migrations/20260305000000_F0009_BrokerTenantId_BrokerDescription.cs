using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0009_BrokerTenantId_BrokerDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F0009: BrokerUser scope resolution — links broker_tenant_id JWT claim to a Broker row.
            migrationBuilder.AddColumn<string>(
                name: "BrokerTenantId",
                table: "Brokers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_BrokerTenantId",
                table: "Brokers",
                column: "BrokerTenantId",
                unique: true,
                filter: "\"BrokerTenantId\" IS NOT NULL");

            // F0009: Broker-safe public description for BrokerUser-visible timeline events.
            migrationBuilder.AddColumn<string>(
                name: "BrokerDescription",
                table: "ActivityTimelineEvents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BrokerDescription",
                table: "ActivityTimelineEvents");

            migrationBuilder.DropIndex(
                name: "IX_Brokers_BrokerTenantId",
                table: "Brokers");

            migrationBuilder.DropColumn(
                name: "BrokerTenantId",
                table: "Brokers");
        }
    }
}
