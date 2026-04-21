using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PrimaryState = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTimelineEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EventPayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    EventDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTimelineEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MGAs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MGAs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceRenewalStatuses",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<short>(type: "smallint", nullable: false),
                    ColorGroup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceRenewalStatuses", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceSubmissionStatuses",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsTerminal = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<short>(type: "smallint", nullable: false),
                    ColorGroup = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceSubmissionStatuses", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceTaskStatuses",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceTaskStatuses", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Normal"),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignedTo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LinkedEntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LinkedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RegionsJson = table.Column<string>(type: "jsonb", nullable: false),
                    RolesJson = table.Column<string>(type: "jsonb", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTransitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromState = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ToState = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActorSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProgramCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MgaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagedBySubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Programs_MGAs_MgaId",
                        column: x => x.MgaId,
                        principalTable: "MGAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Brokers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    State = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ManagedBySubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    MgaId = table.Column<Guid>(type: "uuid", nullable: true),
                    PrimaryProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brokers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Brokers_MGAs_MgaId",
                        column: x => x.MgaId,
                        principalTable: "MGAs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Brokers_Programs_PrimaryProgramId",
                        column: x => x.PrimaryProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BrokerRegions",
                columns: table => new
                {
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerRegions", x => new { x.BrokerId, x.Region });
                    table.ForeignKey(
                        name: "FK_BrokerRegions_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contacts_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Received"),
                    EffectiveDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PremiumEstimate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AssignedTo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_Programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "Programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Renewals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    BrokerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Created"),
                    RenewalDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AssignedTo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Renewals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Renewals_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Renewals_Brokers_BrokerId",
                        column: x => x.BrokerId,
                        principalTable: "Brokers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Renewals_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ReferenceRenewalStatuses",
                columns: new[] { "Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal" },
                values: new object[,]
                {
                    { "Bound", null, "Policy renewed and bound", "Bound", (short)6, true },
                    { "Created", "intake", "Renewal record created from expiring policy", "Created", (short)1, false },
                    { "Early", "intake", "In early renewal window (90-120 days out)", "Early", (short)2, false },
                    { "InReview", "review", "Under underwriter review for renewal terms", "In Review", (short)4, false },
                    { "Lapsed", null, "Policy expired without renewal", "Lapsed", (short)8, true },
                    { "Lost", null, "Lost to competitor", "Lost", (short)7, true },
                    { "OutreachStarted", "waiting", "Active broker/account outreach begun", "Outreach Started", (short)3, false },
                    { "Quoted", "decision", "Renewal quote issued", "Quoted", (short)5, false }
                });

            migrationBuilder.InsertData(
                table: "ReferenceSubmissionStatuses",
                columns: new[] { "Code", "ColorGroup", "Description", "DisplayName", "DisplayOrder", "IsTerminal" },
                values: new object[,]
                {
                    { "BindRequested", "decision", "Broker accepted quote, bind in progress", "Bind Requested", (short)7, false },
                    { "Bound", null, "Policy bound and issued", "Bound", (short)8, true },
                    { "Declined", null, "Submission declined by underwriter", "Declined", (short)9, true },
                    { "InReview", "review", "Under active underwriter review", "In Review", (short)5, false },
                    { "Quoted", "decision", "Quote issued, awaiting broker response", "Quoted", (short)6, false },
                    { "ReadyForUWReview", "review", "All data received, queued for underwriter", "Ready for UW Review", (short)4, false },
                    { "Received", "intake", "Initial state when submission is created", "Received", (short)1, false },
                    { "Triaging", "triage", "Initial triage and data validation", "Triaging", (short)2, false },
                    { "WaitingOnBroker", "waiting", "Awaiting additional information from broker", "Waiting on Broker", (short)3, false },
                    { "Withdrawn", null, "Broker withdrew submission", "Withdrawn", (short)10, true }
                });

            migrationBuilder.InsertData(
                table: "ReferenceTaskStatuses",
                columns: new[] { "Code", "DisplayName", "DisplayOrder" },
                values: new object[,]
                {
                    { "Done", "Done", (short)3 },
                    { "InProgress", "In Progress", (short)2 },
                    { "Open", "Open", (short)1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Region",
                table: "Accounts",
                column: "Region");

            migrationBuilder.CreateIndex(
                name: "IX_ATE_EntityType_OccurredAt",
                table: "ActivityTimelineEvents",
                columns: new[] { "EntityType", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerRegions_Region_BrokerId",
                table: "BrokerRegions",
                columns: new[] { "Region", "BrokerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_LicenseNumber",
                table: "Brokers",
                column: "LicenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_ManagedBySubject",
                table: "Brokers",
                column: "ManagedBySubject");

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_MgaId",
                table: "Brokers",
                column: "MgaId");

            migrationBuilder.CreateIndex(
                name: "IX_Brokers_PrimaryProgramId",
                table: "Brokers",
                column: "PrimaryProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_AccountId",
                table: "Contacts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_BrokerId",
                table: "Contacts",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_ManagedBySubject",
                table: "Programs",
                column: "ManagedBySubject");

            migrationBuilder.CreateIndex(
                name: "IX_Programs_MgaId",
                table: "Programs",
                column: "MgaId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceRenewalStatuses_DisplayOrder",
                table: "ReferenceRenewalStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceSubmissionStatuses_DisplayOrder",
                table: "ReferenceSubmissionStatuses",
                column: "DisplayOrder",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_AccountId",
                table: "Renewals",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_AssignedTo_CurrentStatus",
                table: "Renewals",
                columns: new[] { "AssignedTo", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_BrokerId",
                table: "Renewals",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_CurrentStatus",
                table: "Renewals",
                column: "CurrentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_RenewalDate_Status",
                table: "Renewals",
                columns: new[] { "RenewalDate", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Renewals_SubmissionId",
                table: "Renewals",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AccountId",
                table: "Submissions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_AssignedTo_CurrentStatus",
                table: "Submissions",
                columns: new[] { "AssignedTo", "CurrentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_BrokerId",
                table: "Submissions",
                column: "BrokerId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CurrentStatus",
                table: "Submissions",
                column: "CurrentStatus",
                filter: "\"CurrentStatus\" NOT IN ('Bound', 'Declined', 'Withdrawn')");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProgramId",
                table: "Submissions",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_AssignedTo_Status_DueDate",
                table: "Tasks",
                columns: new[] { "AssignedTo", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_DueDate_Status",
                table: "Tasks",
                columns: new[] { "DueDate", "Status" },
                filter: "\"IsDeleted\" = false AND \"Status\" != 'Done'");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_LinkedEntity",
                table: "Tasks",
                columns: new[] { "LinkedEntityType", "LinkedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Subject",
                table: "UserProfiles",
                column: "Subject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WT_EntityId_OccurredAt",
                table: "WorkflowTransitions",
                columns: new[] { "EntityId", "OccurredAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityTimelineEvents");

            migrationBuilder.DropTable(
                name: "BrokerRegions");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "ReferenceRenewalStatuses");

            migrationBuilder.DropTable(
                name: "ReferenceSubmissionStatuses");

            migrationBuilder.DropTable(
                name: "ReferenceTaskStatuses");

            migrationBuilder.DropTable(
                name: "Renewals");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "WorkflowTransitions");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "Brokers");

            migrationBuilder.DropTable(
                name: "Programs");

            migrationBuilder.DropTable(
                name: "MGAs");
        }
    }
}
