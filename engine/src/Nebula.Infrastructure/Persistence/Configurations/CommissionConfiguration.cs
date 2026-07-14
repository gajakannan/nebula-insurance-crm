using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CommissionScheduleConfiguration : IEntityTypeConfiguration<CommissionSchedule>
{
    public void Configure(EntityTypeBuilder<CommissionSchedule> builder)
    {
        builder.ToTable("CommissionSchedules");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LineOfBusiness).IsRequired().HasMaxLength(120);
        builder.Property(e => e.State).HasMaxLength(2);
        builder.Property(e => e.ProductCode).HasMaxLength(80);
        builder.Property(e => e.Basis).IsRequired().HasMaxLength(40);
        builder.Property(e => e.RatePercent).HasPrecision(9, 4);
        builder.Property(e => e.FlatAmount).HasPrecision(18, 2);
        builder.Property(e => e.SourceNote).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => new { e.CarrierMarketId, e.LineOfBusiness, e.State, e.ProductCode, e.EffectiveFrom, e.EffectiveTo })
            .HasDatabaseName("IX_CommissionSchedules_Scope_Effective");
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<CommissionSchedule> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class ProducerSplitAssignmentConfiguration : IEntityTypeConfiguration<ProducerSplitAssignment>
{
    public void Configure(EntityTypeBuilder<ProducerSplitAssignment> builder)
    {
        builder.ToTable("ProducerSplitAssignments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasMany(e => e.Participants)
            .WithOne(e => e.Assignment)
            .HasForeignKey(e => e.ProducerSplitAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.PolicyId, e.EffectiveFrom, e.EffectiveTo })
            .HasDatabaseName("IX_ProducerSplitAssignments_Policy_Effective");
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<ProducerSplitAssignment> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class ProducerSplitParticipantConfiguration : IEntityTypeConfiguration<ProducerSplitParticipant>
{
    public void Configure(EntityTypeBuilder<ProducerSplitParticipant> builder)
    {
        builder.ToTable("ProducerSplitParticipants");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SplitPercent).HasPrecision(9, 4);
        builder.Property(e => e.SourceOwnershipSnapshotJson).HasColumnType("jsonb");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => new { e.ProducerSplitAssignmentId, e.ProducerId }).IsUnique()
            .HasDatabaseName("IX_ProducerSplitParticipants_Assignment_Producer");
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<ProducerSplitParticipant> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class ExpectedCommissionConfiguration : IEntityTypeConfiguration<ExpectedCommission>
{
    public void Configure(EntityTypeBuilder<ExpectedCommission> builder)
    {
        builder.ToTable("ExpectedCommissions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.PremiumBasisAmount).HasPrecision(18, 2);
        builder.Property(e => e.ExpectedGrossCommission).HasPrecision(18, 2);
        builder.Property(e => e.ApprovedAdjustmentTotal).HasPrecision(18, 2);
        builder.Property(e => e.AdjustedExpectedCommission).HasPrecision(18, 2);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(40);
        builder.Property(e => e.ExceptionState).IsRequired().HasMaxLength(80);
        builder.Property(e => e.SourceSnapshotJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasMany(e => e.Adjustments)
            .WithOne(e => e.ExpectedCommission)
            .HasForeignKey(e => e.ExpectedCommissionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.PolicyId, e.Status, e.ExceptionState }).HasDatabaseName("IX_ExpectedCommissions_Policy_Status_Exception");
        builder.HasIndex(e => new { e.CarrierMarketId, e.Status }).HasDatabaseName("IX_ExpectedCommissions_Carrier_Status");
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<ExpectedCommission> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class CommissionAdjustmentConfiguration : IEntityTypeConfiguration<CommissionAdjustment>
{
    public void Configure(EntityTypeBuilder<CommissionAdjustment> builder)
    {
        builder.ToTable("CommissionAdjustments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(40);
        builder.Property(e => e.DecisionNote).HasMaxLength(1000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => new { e.ExpectedCommissionId, e.Status }).HasDatabaseName("IX_CommissionAdjustments_Commission_Status");
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<CommissionAdjustment> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}

public class RevenueAttributionProjectionConfiguration : IEntityTypeConfiguration<RevenueAttributionProjection>
{
    public void Configure(EntityTypeBuilder<RevenueAttributionProjection> builder)
    {
        builder.ToTable("RevenueAttributionProjections");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LineOfBusiness).IsRequired().HasMaxLength(120);
        builder.Property(e => e.ExpectedGrossCommission).HasPrecision(18, 2);
        builder.Property(e => e.ApprovedAdjustmentTotal).HasPrecision(18, 2);
        builder.Property(e => e.AdjustedExpectedCommission).HasPrecision(18, 2);
        builder.Property(e => e.ProducerAllocationAmount).HasPrecision(18, 2);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.HasIndex(e => e.ExpectedCommissionId).IsUnique().HasDatabaseName("IX_RevenueAttributionProjections_Commission");
        builder.HasIndex(e => new { e.PolicyPeriodStart, e.PolicyPeriodEnd }).HasDatabaseName("IX_RevenueAttributionProjections_Period");
        builder.HasIndex(e => new { e.ProducerId, e.BrokerId, e.TerritoryId, e.CarrierMarketId }).HasDatabaseName("IX_RevenueAttributionProjections_Dimensions");
        ConfigureRowVersion(builder);
        builder.HasQueryFilter(e => !e.IsDeleted);
    }

    private static void ConfigureRowVersion(EntityTypeBuilder<RevenueAttributionProjection> builder) =>
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
}
