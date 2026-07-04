using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class BrokerInsightProjectionConfiguration : IEntityTypeConfiguration<BrokerInsightProjection>
{
    public void Configure(EntityTypeBuilder<BrokerInsightProjection> builder)
    {
        builder.ToTable("BrokerInsightProjections");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.BrokerName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.MetricKey).IsRequired().HasMaxLength(80);
        builder.Property(e => e.MetricLabel).IsRequired().HasMaxLength(120);
        builder.Property(e => e.MetricFamily).IsRequired().HasMaxLength(80);
        builder.Property(e => e.Bucket).HasMaxLength(20);
        builder.Property(e => e.Unit).IsRequired().HasMaxLength(20);
        builder.Property(e => e.SourceObjectTypesJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.LineOfBusiness).HasMaxLength(80);
        builder.Property(e => e.Region).HasMaxLength(80);
        builder.Property(e => e.ProjectionStatus).IsRequired().HasMaxLength(40);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.BrokerId, e.PeriodStart, e.PeriodEnd })
            .HasDatabaseName("IX_BrokerInsight_Broker_Period");
        builder.HasIndex(e => new { e.MetricKey, e.PeriodStart, e.PeriodEnd })
            .HasDatabaseName("IX_BrokerInsight_Metric_Period");
        builder.HasIndex(e => new { e.ProgramId, e.ProducerId, e.TerritoryId, e.Region, e.LineOfBusiness })
            .HasDatabaseName("IX_BrokerInsight_Dimensions");
        builder.HasIndex(e => e.ProjectedAt).HasDatabaseName("IX_BrokerInsight_ProjectedAt");
    }
}
