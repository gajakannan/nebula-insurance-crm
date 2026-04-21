using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class BrokerRegionConfiguration : IEntityTypeConfiguration<BrokerRegion>
{
    public void Configure(EntityTypeBuilder<BrokerRegion> builder)
    {
        builder.ToTable("BrokerRegions");

        builder.HasKey(e => new { e.BrokerId, e.Region });

        builder.Property(e => e.Region).IsRequired().HasMaxLength(50);

        builder.HasOne(e => e.Broker)
            .WithMany(b => b.BrokerRegions)
            .HasForeignKey(e => e.BrokerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.Region, e.BrokerId })
            .HasDatabaseName("IX_BrokerRegions_Region_BrokerId");
    }
}
