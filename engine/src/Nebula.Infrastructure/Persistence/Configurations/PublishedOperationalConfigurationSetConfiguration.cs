using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class PublishedOperationalConfigurationSetConfiguration : IEntityTypeConfiguration<PublishedOperationalConfigurationSet>
{
    public void Configure(EntityTypeBuilder<PublishedOperationalConfigurationSet> builder)
    {
        builder.ToTable("PublishedOperationalConfigurationSets");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DomainKey).IsRequired().HasMaxLength(80);
        builder.Property(e => e.PayloadSnapshotJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.PayloadHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.PublishReason).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasOne(e => e.Domain).WithMany(e => e.PublishedSets).HasForeignKey(e => e.DomainKey).HasPrincipalKey(e => e.DomainKey).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.DomainKey, e.PublishedVersion }).IsUnique().HasDatabaseName("UX_PublishedOperationalConfigurationSets_Domain_Version");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
