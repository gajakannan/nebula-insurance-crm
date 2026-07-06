using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ConfigurationRefreshStatusConfiguration : IEntityTypeConfiguration<ConfigurationRefreshStatus>
{
    public void Configure(EntityTypeBuilder<ConfigurationRefreshStatus> builder)
    {
        builder.ToTable("ConfigurationRefreshStatuses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ConsumerKey).IsRequired().HasMaxLength(80);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.ErrorSummary).HasMaxLength(1000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasOne(e => e.PublishedSet).WithMany(e => e.RefreshStatuses).HasForeignKey(e => e.PublishedSetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.PublishedSetId, e.ConsumerKey }).HasDatabaseName("IX_ConfigurationRefreshStatuses_PublishedSet_Consumer");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
