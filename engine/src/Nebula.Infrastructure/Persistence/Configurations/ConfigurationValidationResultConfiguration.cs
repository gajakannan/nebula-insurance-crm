using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ConfigurationValidationResultConfiguration : IEntityTypeConfiguration<ConfigurationValidationResult>
{
    public void Configure(EntityTypeBuilder<ConfigurationValidationResult> builder)
    {
        builder.ToTable("ConfigurationValidationResults");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.DraftPayloadHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.BlockingErrorsJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.WarningsJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.CompareSummaryJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasOne(e => e.Draft).WithMany(e => e.ValidationResults).HasForeignKey(e => e.DraftId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => new { e.DraftId, e.CreatedAt }).HasDatabaseName("IX_ConfigurationValidationResults_Draft_CreatedAt");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
