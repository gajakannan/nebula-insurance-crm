using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Nebula.Infrastructure.Persistence;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260703171000_F0024_ServiceCases")]
    public partial class F0024_ServiceCases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: true),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Intake"),
                    Priority = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Medium"),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FollowUpSummary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ServiceCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCases_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCases_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCaseClaimReferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierClaimNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DateOfLoss = table.Column<DateOnly>(type: "date", nullable: true),
                    ClaimantDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LossSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CarrierContactReference = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_ServiceCaseClaimReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCaseClaimReferences_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCaseCommunicationLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    CommunicationEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "Context"),
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
                    table.PrimaryKey("PK_ServiceCaseCommunicationLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCaseCommunicationLinks_CommunicationEvents_CommunicationEventId",
                        column: x => x.CommunicationEventId,
                        principalTable: "CommunicationEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceCaseCommunicationLinks_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCaseTaskLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Relationship = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, defaultValue: "FollowUp"),
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
                    table.PrimaryKey("PK_ServiceCaseTaskLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCaseTaskLinks_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceCaseTaskLinks_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceCaseTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReasonCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ServiceCaseTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceCaseTransitions_ServiceCases_ServiceCaseId",
                        column: x => x.ServiceCaseId,
                        principalTable: "ServiceCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseClaimReferences_CarrierClaimNumber",
                table: "ServiceCaseClaimReferences",
                column: "CarrierClaimNumber");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceCaseClaimReferences_ServiceCase",
                table: "ServiceCaseClaimReferences",
                column: "ServiceCaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseCommunicationLinks_CommunicationEventId",
                table: "ServiceCaseCommunicationLinks",
                column: "CommunicationEventId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceCaseCommunicationLinks_Case_Event",
                table: "ServiceCaseCommunicationLinks",
                columns: new[] { "ServiceCaseId", "CommunicationEventId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_Account_Status",
                table: "ServiceCases",
                columns: new[] { "AccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_Owner_Status_DueDate",
                table: "ServiceCases",
                columns: new[] { "OwnerUserId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCases_Policy_Status",
                table: "ServiceCases",
                columns: new[] { "PolicyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiceCases_CaseNumber",
                table: "ServiceCases",
                column: "CaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseTaskLinks_TaskId",
                table: "ServiceCaseTaskLinks",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiceCaseTaskLinks_Case_Task",
                table: "ServiceCaseTaskLinks",
                columns: new[] { "ServiceCaseId", "TaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCaseTransitions_Case_OccurredAt",
                table: "ServiceCaseTransitions",
                columns: new[] { "ServiceCaseId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ServiceCaseClaimReferences");
            migrationBuilder.DropTable(name: "ServiceCaseCommunicationLinks");
            migrationBuilder.DropTable(name: "ServiceCaseTaskLinks");
            migrationBuilder.DropTable(name: "ServiceCaseTransitions");
            migrationBuilder.DropTable(name: "ServiceCases");
        }
    }
}
