using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class OperationalReportProjectionConfiguration : IEntityTypeConfiguration<OperationalReportProjection>
{
    public void Configure(EntityTypeBuilder<OperationalReportProjection> builder)
    {
        builder.ToTable("OperationalReportProjections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SourceObjectType).IsRequired().HasMaxLength(40);
        builder.Property(e => e.TargetUrl).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.WorkflowType).HasMaxLength(60);
        builder.Property(e => e.CurrentStatus).HasMaxLength(60);
        builder.Property(e => e.OwnerDisplayName).HasMaxLength(200);
        builder.Property(e => e.AgeBand).HasMaxLength(40);
        builder.Property(e => e.LineOfBusiness).HasMaxLength(120);
        builder.Property(e => e.Region).HasMaxLength(120);
        builder.Property(e => e.DueDate).HasColumnType("date");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.SourceObjectType, e.SourceObjectId })
            .IsUnique()
            .HasDatabaseName("IX_OperationalReportProjections_Source_Unique");

        builder.HasIndex(e => e.OwnerUserId).HasDatabaseName("IX_OperationalReportProjections_OwnerUserId");
        builder.HasIndex(e => new { e.WorkflowType, e.CurrentStatus }).HasDatabaseName("IX_OperationalReportProjections_Workflow_Status");
        builder.HasIndex(e => e.AgeBand).HasDatabaseName("IX_OperationalReportProjections_AgeBand");
        builder.HasIndex(e => e.DueDate).HasDatabaseName("IX_OperationalReportProjections_DueDate");
        builder.HasIndex(e => e.Region).HasDatabaseName("IX_OperationalReportProjections_Region");
        builder.HasIndex(e => e.LineOfBusiness).HasDatabaseName("IX_OperationalReportProjections_LineOfBusiness");
        builder.HasIndex(e => e.ProgramId).HasDatabaseName("IX_OperationalReportProjections_ProgramId");
        builder.HasIndex(e => e.TerritoryId).HasDatabaseName("IX_OperationalReportProjections_TerritoryId");
        builder.HasIndex(e => e.BrokerId).HasDatabaseName("IX_OperationalReportProjections_BrokerId");
        builder.HasIndex(e => e.AccountId).HasDatabaseName("IX_OperationalReportProjections_AccountId");
    }
}
