using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class BillingInvoiceConfiguration : IEntityTypeConfiguration<BillingInvoice>
{
    public void Configure(EntityTypeBuilder<BillingInvoice> builder)
    {
        builder.ToTable("BillingInvoices", table =>
        {
            table.HasCheckConstraint("CK_BillingInvoices_Amounts", "\"OriginalAmount\" > 0 AND \"OutstandingAmount\" >= 0 AND \"OutstandingAmount\" <= \"OriginalAmount\"");
            table.HasCheckConstraint("CK_BillingInvoices_Dates", "\"DueDate\" >= \"InvoiceDate\"");
            table.HasCheckConstraint("CK_BillingInvoices_Status", "\"Status\" IN ('Outstanding', 'Reconciled')");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.InvoiceNumber).IsRequired().HasMaxLength(80);
        builder.Property(e => e.NormalizedInvoiceNumber).IsRequired().HasMaxLength(80);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.OriginalAmount).HasPrecision(18, 2);
        builder.Property(e => e.OutstandingAmount).HasPrecision(18, 2);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => e.NormalizedInvoiceNumber).IsUnique();
        builder.HasIndex(e => new { e.PolicyId, e.InvoiceDate });
        builder.HasIndex(e => new { e.AccountId, e.Status, e.DueDate });
        builder.HasIndex(e => new { e.Status, e.DueDate });
        builder.HasOne(e => e.Policy).WithMany().HasForeignKey(e => e.PolicyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PolicyVersion).WithMany().HasForeignKey(e => e.PolicyVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<BillingInvoice> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class PaymentReceiptConfiguration : IEntityTypeConfiguration<PaymentReceipt>
{
    public void Configure(EntityTypeBuilder<PaymentReceipt> builder)
    {
        builder.ToTable("PaymentReceipts", table =>
        {
            table.HasCheckConstraint("CK_PaymentReceipts_Amount", "\"Amount\" > 0");
            table.HasCheckConstraint("CK_PaymentReceipts_Source", "\"Source\" IN ('Manual', 'MockVendorCsv')");
            table.HasCheckConstraint("CK_PaymentReceipts_ApplicationStatus", "\"ApplicationStatus\" IN ('Unapplied', 'Applied')");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Source).IsRequired().HasMaxLength(32);
        builder.Property(e => e.ExternalReference).IsRequired().HasMaxLength(120);
        builder.Property(e => e.NormalizedExternalReference).IsRequired().HasMaxLength(120);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.InvoiceReference).HasMaxLength(80);
        builder.Property(e => e.Memo).HasMaxLength(500);
        builder.Property(e => e.ApplicationStatus).IsRequired().HasMaxLength(32);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => new { e.Source, e.NormalizedExternalReference }).IsUnique();
        builder.HasIndex(e => new { e.ApplicationStatus, e.ReceivedDate });
        builder.HasOne(e => e.ImportBatch).WithMany(e => e.Receipts).HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<PaymentReceipt> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class PaymentReceiptImportBatchConfiguration : IEntityTypeConfiguration<PaymentReceiptImportBatch>
{
    public void Configure(EntityTypeBuilder<PaymentReceiptImportBatch> builder)
    {
        builder.ToTable("PaymentReceiptImportBatches");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ContractVersion).IsRequired().HasMaxLength(80);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.FileSha256).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => new { e.ImportedAt, e.ImportedByUserId });
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<PaymentReceiptImportBatch> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class PaymentReceiptImportRowOutcomeConfiguration : IEntityTypeConfiguration<PaymentReceiptImportRowOutcome>
{
    public void Configure(EntityTypeBuilder<PaymentReceiptImportRowOutcome> builder)
    {
        builder.ToTable("PaymentReceiptImportRowOutcomes", table =>
            table.HasCheckConstraint("CK_PaymentReceiptImportRowOutcomes_Outcome", "\"Outcome\" IN ('Created', 'Duplicate', 'Rejected')"));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ExternalReference).HasMaxLength(120);
        builder.Property(e => e.Outcome).IsRequired().HasMaxLength(32);
        builder.Property(e => e.ReasonCode).HasMaxLength(80);
        builder.Property(e => e.ReasonDetail).HasMaxLength(500);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => new { e.ImportBatchId, e.RowNumber }).IsUnique();
        builder.HasOne(e => e.ImportBatch).WithMany(e => e.Outcomes).HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.PaymentReceipt).WithMany().HasForeignKey(e => e.PaymentReceiptId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<PaymentReceiptImportRowOutcome> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class PaymentApplicationConfiguration : IEntityTypeConfiguration<PaymentApplication>
{
    public void Configure(EntityTypeBuilder<PaymentApplication> builder)
    {
        builder.ToTable("PaymentApplications", table =>
        {
            table.HasCheckConstraint("CK_PaymentApplications_Amount", "\"AppliedAmount\" > 0");
            table.HasCheckConstraint("CK_PaymentApplications_After", "\"InvoiceOutstandingAfter\" = 0");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(e => e.AppliedAmount).HasPrecision(18, 2);
        builder.Property(e => e.InvoiceOutstandingBefore).HasPrecision(18, 2);
        builder.Property(e => e.InvoiceOutstandingAfter).HasPrecision(18, 2);
        builder.HasIndex(e => e.BillingInvoiceId).IsUnique();
        builder.HasIndex(e => e.PaymentReceiptId).IsUnique();
        builder.HasOne(e => e.BillingInvoice).WithOne(e => e.PaymentApplication).HasForeignKey<PaymentApplication>(e => e.BillingInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PaymentReceipt).WithOne(e => e.PaymentApplication).HasForeignKey<PaymentApplication>(e => e.PaymentReceiptId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ReconciliationExceptionConfiguration : IEntityTypeConfiguration<ReconciliationException>
{
    public void Configure(EntityTypeBuilder<ReconciliationException> builder)
    {
        builder.ToTable("ReconciliationExceptions", table =>
            table.HasCheckConstraint("CK_ReconciliationExceptions_Status", "\"Status\" IN ('Open', 'Resolved')"));
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.ResolutionCode).HasMaxLength(80);
        builder.Property(e => e.ResolutionNote).HasMaxLength(1000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => new { e.Status, e.Type, e.OpenedAt });
        builder.HasIndex(e => e.BillingInvoiceId);
        builder.HasIndex(e => e.PaymentReceiptId);
        builder.HasOne(e => e.BillingInvoice).WithMany(e => e.ReconciliationExceptions).HasForeignKey(e => e.BillingInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.PaymentReceipt).WithMany(e => e.ReconciliationExceptions).HasForeignKey(e => e.PaymentReceiptId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ImportBatch).WithMany().HasForeignKey(e => e.ImportBatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.ImportRowOutcome).WithMany().HasForeignKey(e => e.ImportRowOutcomeId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<ReconciliationException> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class BillingCorrectionConfiguration : IEntityTypeConfiguration<BillingCorrection>
{
    public void Configure(EntityTypeBuilder<BillingCorrection> builder)
    {
        builder.ToTable("BillingCorrections", table =>
        {
            table.HasCheckConstraint("CK_BillingCorrections_Status", "\"Status\" IN ('Pending', 'Approved', 'Rejected')");
            table.HasCheckConstraint("CK_BillingCorrections_Proposed", "\"ProposedOutstandingAmount\" >= 0");
        });
        builder.HasKey(e => e.Id);
        builder.Property(e => e.BeforeOutstandingAmount).HasPrecision(18, 2);
        builder.Property(e => e.CorrectionAmount).HasPrecision(18, 2);
        builder.Property(e => e.ProposedOutstandingAmount).HasPrecision(18, 2);
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.EvidenceNote).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(32);
        builder.Property(e => e.DecisionNote).HasMaxLength(1000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => e.ReconciliationExceptionId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Pending'");
        builder.HasIndex(e => new { e.BillingInvoiceId, e.Status });
        builder.HasOne(e => e.ReconciliationException).WithMany(e => e.Corrections).HasForeignKey(e => e.ReconciliationExceptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.BillingInvoice).WithMany(e => e.Corrections).HasForeignKey(e => e.BillingInvoiceId).OnDelete(DeleteBehavior.Restrict);
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<BillingCorrection> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}
