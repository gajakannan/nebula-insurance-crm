using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ConfigurationDraftConfiguration : IEntityTypeConfiguration<ConfigurationDraft>
{
    public void Configure(EntityTypeBuilder<ConfigurationDraft> builder)
    {
        builder.ToTable("ConfigurationDrafts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DomainKey).IsRequired().HasMaxLength(80);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.PayloadJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.PayloadHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasOne(e => e.Domain).WithMany(e => e.Drafts).HasForeignKey(e => e.DomainKey).HasPrincipalKey(e => e.DomainKey).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.DomainKey, e.Status }).HasDatabaseName("IX_ConfigurationDrafts_Domain_Status");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
