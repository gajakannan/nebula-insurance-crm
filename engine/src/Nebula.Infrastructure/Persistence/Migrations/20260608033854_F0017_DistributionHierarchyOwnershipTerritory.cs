using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0017_DistributionHierarchyOwnershipTerritory : Migration
    {
        // F0017 (ADR-026): distribution hierarchy + effective-dated producer ownership and territory.
        // Scoped to the four F0017 tables only. Filtered unique indexes (single-open-period, active
        // territory name, single-open-member-assignment) are added via raw SQL — not expressible via the fluent API.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DistributionNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    AncestryPath = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
                    Depth = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ChildCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DistributionNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DistributionNodes_DistributionNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "DistributionNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Territories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriteriaJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Territories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProducerOwnership",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ScopeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProducerNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    AssignmentReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProducerOwnership", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProducerOwnership_DistributionNodes_ProducerNodeId",
                        column: x => x.ProducerNodeId,
                        principalTable: "DistributionNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TerritoryAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TerritoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    AssignmentReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TerritoryAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TerritoryAssignments_Territories_TerritoryId",
                        column: x => x.TerritoryId,
                        principalTable: "Territories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DistributionNodes_AncestryPath",
                table: "DistributionNodes",
                column: "AncestryPath");

            migrationBuilder.CreateIndex(
                name: "IX_DistributionNodes_NodeType_IsActive",
                table: "DistributionNodes",
                columns: new[] { "NodeType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DistributionNodes_ParentId",
                table: "DistributionNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProducerOwnership_ProducerNodeId",
                table: "ProducerOwnership",
                column: "ProducerNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProducerOwnership_Scope",
                table: "ProducerOwnership",
                columns: new[] { "ScopeType", "ScopeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Territories_Name",
                table: "Territories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryAssignments_Member_AsOf",
                table: "TerritoryAssignments",
                columns: new[] { "MemberType", "MemberId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_TerritoryAssignments_TerritoryId",
                table: "TerritoryAssignments",
                column: "TerritoryId");

            // Filtered unique indexes — effective-dating + active-name invariants (ADR-026 §3/§4).
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_ProducerOwnership_OpenPeriod\" " +
                "ON \"ProducerOwnership\" (\"ScopeType\", \"ScopeId\") " +
                "WHERE \"EffectiveTo\" IS NULL AND \"IsDeleted\" = false;");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_Territories_ActiveName\" " +
                "ON \"Territories\" (LOWER(\"Name\")) " +
                "WHERE \"IsActive\" = true AND \"IsDeleted\" = false;");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX \"IX_TerritoryAssignments_OpenPeriod\" " +
                "ON \"TerritoryAssignments\" (\"MemberType\", \"MemberId\") " +
                "WHERE \"EffectiveTo\" IS NULL AND \"IsDeleted\" = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProducerOwnership");
            migrationBuilder.DropTable(name: "TerritoryAssignments");
            migrationBuilder.DropTable(name: "Territories");
            migrationBuilder.DropTable(name: "DistributionNodes");
        }
    }
}
