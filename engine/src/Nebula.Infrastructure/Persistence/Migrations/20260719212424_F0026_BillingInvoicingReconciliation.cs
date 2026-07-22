using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nebula.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class F0026_BillingInvoicingReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingInvoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NormalizedInvoiceNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PolicyVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_BillingInvoices", x => x.Id);
                    table.CheckConstraint("CK_BillingInvoices_Amounts", "\"OriginalAmount\" > 0 AND \"OutstandingAmount\" >= 0 AND \"OutstandingAmount\" <= \"OriginalAmount\"");
                    table.CheckConstraint("CK_BillingInvoices_Dates", "\"DueDate\" >= \"InvoiceDate\"");
                    table.CheckConstraint("CK_BillingInvoices_Status", "\"Status\" IN ('Outstanding', 'Reconciled')");
                    table.ForeignKey(
                        name: "FK_BillingInvoices_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_Policies_PolicyId",
                        column: x => x.PolicyId,
                        principalTable: "Policies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingInvoices_PolicyVersions_PolicyVersionId",
                        column: x => x.PolicyVersionId,
                        principalTable: "PolicyVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReceiptImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContractVersion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileSha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SubmittedCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedCount = table.Column<int>(type: "integer", nullable: false),
                    DuplicateCount = table.Column<int>(type: "integer", nullable: false),
                    RejectedCount = table.Column<int>(type: "integer", nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ImportedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PaymentReceiptImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NormalizedExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceReference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Memo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImportRowNumber = table.Column<int>(type: "integer", nullable: true),
                    ApplicationStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("PK_PaymentReceipts", x => x.Id);
                    table.CheckConstraint("CK_PaymentReceipts_Amount", "\"Amount\" > 0");
                    table.CheckConstraint("CK_PaymentReceipts_ApplicationStatus", "\"ApplicationStatus\" IN ('Unapplied', 'Applied')");
                    table.CheckConstraint("CK_PaymentReceipts_Source", "\"Source\" IN ('Manual', 'MockVendorCsv')");
                    table.ForeignKey(
                        name: "FK_PaymentReceipts_PaymentReceiptImportBatches_ImportBatchId",
                        column: x => x.ImportBatchId,
                        principalTable: "PaymentReceiptImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: false),
                    AppliedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceOutstandingBefore = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InvoiceOutstandingAfter = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AppliedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentApplications", x => x.Id);
                    table.CheckConstraint("CK_PaymentApplications_After", "\"InvoiceOutstandingAfter\" = 0");
                    table.CheckConstraint("CK_PaymentApplications_Amount", "\"AppliedAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_PaymentApplications_BillingInvoices_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentApplications_PaymentReceipts_PaymentReceiptId",
                        column: x => x.PaymentReceiptId,
                        principalTable: "PaymentReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentReceiptImportRowOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<int>(type: "integer", nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PaymentReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReasonCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ReasonDetail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PaymentReceiptImportRowOutcomes", x => x.Id);
                    table.CheckConstraint("CK_PaymentReceiptImportRowOutcomes_Outcome", "\"Outcome\" IN ('Created', 'Duplicate', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_PaymentReceiptImportRowOutcomes_PaymentReceiptImportBatches~",
                        column: x => x.ImportBatchId,
                        principalTable: "PaymentReceiptImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentReceiptImportRowOutcomes_PaymentReceipts_PaymentRece~",
                        column: x => x.PaymentReceiptId,
                        principalTable: "PaymentReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationExceptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BillingInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImportBatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImportRowOutcomeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpenedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResolutionCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    ResolutionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_ReconciliationExceptions", x => x.Id);
                    table.CheckConstraint("CK_ReconciliationExceptions_Status", "\"Status\" IN ('Open', 'Resolved')");
                    table.ForeignKey(
                        name: "FK_ReconciliationExceptions_BillingInvoices_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationExceptions_PaymentReceiptImportBatches_Import~",
                        column: x => x.ImportBatchId,
                        principalTable: "PaymentReceiptImportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationExceptions_PaymentReceiptImportRowOutcomes_Im~",
                        column: x => x.ImportRowOutcomeId,
                        principalTable: "PaymentReceiptImportRowOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationExceptions_PaymentReceipts_PaymentReceiptId",
                        column: x => x.PaymentReceiptId,
                        principalTable: "PaymentReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BillingCorrections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReconciliationExceptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BeforeOutstandingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CorrectionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ProposedOutstandingAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EvidenceNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DecisionByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_BillingCorrections", x => x.Id);
                    table.CheckConstraint("CK_BillingCorrections_Proposed", "\"ProposedOutstandingAmount\" >= 0");
                    table.CheckConstraint("CK_BillingCorrections_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected')");
                    table.ForeignKey(
                        name: "FK_BillingCorrections_BillingInvoices_BillingInvoiceId",
                        column: x => x.BillingInvoiceId,
                        principalTable: "BillingInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BillingCorrections_ReconciliationExceptions_ReconciliationE~",
                        column: x => x.ReconciliationExceptionId,
                        principalTable: "ReconciliationExceptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingCorrections_BillingInvoiceId_Status",
                table: "BillingCorrections",
                columns: new[] { "BillingInvoiceId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingCorrections_ReconciliationExceptionId",
                table: "BillingCorrections",
                column: "ReconciliationExceptionId",
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_AccountId_Status_DueDate",
                table: "BillingInvoices",
                columns: new[] { "AccountId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_NormalizedInvoiceNumber",
                table: "BillingInvoices",
                column: "NormalizedInvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_PolicyId_InvoiceDate",
                table: "BillingInvoices",
                columns: new[] { "PolicyId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_PolicyVersionId",
                table: "BillingInvoices",
                column: "PolicyVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingInvoices_Status_DueDate",
                table: "BillingInvoices",
                columns: new[] { "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentApplications_BillingInvoiceId",
                table: "PaymentApplications",
                column: "BillingInvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentApplications_PaymentReceiptId",
                table: "PaymentApplications",
                column: "PaymentReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceiptImportBatches_ImportedAt_ImportedByUserId",
                table: "PaymentReceiptImportBatches",
                columns: new[] { "ImportedAt", "ImportedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceiptImportRowOutcomes_ImportBatchId_RowNumber",
                table: "PaymentReceiptImportRowOutcomes",
                columns: new[] { "ImportBatchId", "RowNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceiptImportRowOutcomes_PaymentReceiptId",
                table: "PaymentReceiptImportRowOutcomes",
                column: "PaymentReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_ApplicationStatus_ReceivedDate",
                table: "PaymentReceipts",
                columns: new[] { "ApplicationStatus", "ReceivedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_ImportBatchId",
                table: "PaymentReceipts",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReceipts_Source_NormalizedExternalReference",
                table: "PaymentReceipts",
                columns: new[] { "Source", "NormalizedExternalReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationExceptions_BillingInvoiceId",
                table: "ReconciliationExceptions",
                column: "BillingInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationExceptions_ImportBatchId",
                table: "ReconciliationExceptions",
                column: "ImportBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationExceptions_ImportRowOutcomeId",
                table: "ReconciliationExceptions",
                column: "ImportRowOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationExceptions_PaymentReceiptId",
                table: "ReconciliationExceptions",
                column: "PaymentReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationExceptions_Status_Type_OpenedAt",
                table: "ReconciliationExceptions",
                columns: new[] { "Status", "Type", "OpenedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingCorrections");

            migrationBuilder.DropTable(
                name: "PaymentApplications");

            migrationBuilder.DropTable(
                name: "ReconciliationExceptions");

            migrationBuilder.DropTable(
                name: "BillingInvoices");

            migrationBuilder.DropTable(
                name: "PaymentReceiptImportRowOutcomes");

            migrationBuilder.DropTable(
                name: "PaymentReceipts");

            migrationBuilder.DropTable(
                name: "PaymentReceiptImportBatches");
        }
    }
}
