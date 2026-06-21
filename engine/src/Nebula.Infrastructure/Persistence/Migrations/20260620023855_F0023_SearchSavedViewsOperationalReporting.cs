using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0023_SearchSavedViewsOperationalReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.CreateTable(
                name: "OperationalReportProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceObjectType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SourceObjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    WorkflowType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    CurrentStatus = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    StatusEnteredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DaysInStatus = table.Column<int>(type: "integer", nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsDueToday = table.Column<bool>(type: "boolean", nullable: false),
                    IsOverdue = table.Column<bool>(type: "boolean", nullable: false),
                    AgeBand = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineOfBusiness = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    TerritoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastSourceUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProjectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_OperationalReportProjections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ViewType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamScopeType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TeamScopeKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CriteriaJson = table.Column<string>(type: "jsonb", nullable: false),
                    SortJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastEditedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_SavedViews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ObjectType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ObjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Subtitle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RenewalId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineOfBusiness = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Region = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    TerritoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    MatchedFieldHintsJson = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    SourceUpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastProjectionError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_SearchDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SavedViewAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SavedViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedViewAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedViewAuditEvents_SavedViews_SavedViewId",
                        column: x => x.SavedViewId,
                        principalTable: "SavedViews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_AccountId",
                table: "OperationalReportProjections",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_AgeBand",
                table: "OperationalReportProjections",
                column: "AgeBand");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_BrokerId",
                table: "OperationalReportProjections",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_DueDate",
                table: "OperationalReportProjections",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_LineOfBusiness",
                table: "OperationalReportProjections",
                column: "LineOfBusiness");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_OwnerUserId",
                table: "OperationalReportProjections",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_ProgramId",
                table: "OperationalReportProjections",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_Region",
                table: "OperationalReportProjections",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_Source_Unique",
                table: "OperationalReportProjections",
                columns: new[] { "SourceObjectType", "SourceObjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_TerritoryId",
                table: "OperationalReportProjections",
                column: "TerritoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalReportProjections_Workflow_Status",
                table: "OperationalReportProjections",
                columns: new[] { "WorkflowType", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViewAuditEvents_SavedViewId_OccurredAt",
                table: "SavedViewAuditEvents",
                columns: new[] { "SavedViewId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_OwnerUserId",
                table: "SavedViews",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_Personal_Name",
                table: "SavedViews",
                columns: new[] { "OwnerUserId", "ViewType", "NormalizedName" },
                unique: true,
                filter: "\"Visibility\" = 'Personal' AND \"ArchivedAt\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_Team_Name",
                table: "SavedViews",
                columns: new[] { "TeamScopeType", "TeamScopeKey", "ViewType", "NormalizedName" },
                unique: true,
                filter: "\"Visibility\" = 'Team' AND \"ArchivedAt\" IS NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_TeamScope",
                table: "SavedViews",
                columns: new[] { "TeamScopeType", "TeamScopeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_SavedViews_Visibility_ViewType",
                table: "SavedViews",
                columns: new[] { "Visibility", "ViewType" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_AccountId",
                table: "SearchDocuments",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_BrokerId",
                table: "SearchDocuments",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_LineOfBusiness",
                table: "SearchDocuments",
                column: "LineOfBusiness");

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_Object_Unique",
                table: "SearchDocuments",
                columns: new[] { "ObjectType", "ObjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_OwnerUserId",
                table: "SearchDocuments",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_ProgramId",
                table: "SearchDocuments",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_Region",
                table: "SearchDocuments",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_SearchText_Trgm",
                table: "SearchDocuments",
                column: "SearchText")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchDocuments_TerritoryId",
                table: "SearchDocuments",
                column: "TerritoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalReportProjections");

            migrationBuilder.DropTable(
                name: "SavedViewAuditEvents");

            migrationBuilder.DropTable(
                name: "SearchDocuments");

            migrationBuilder.DropTable(
                name: "SavedViews");
        }
    }
}
