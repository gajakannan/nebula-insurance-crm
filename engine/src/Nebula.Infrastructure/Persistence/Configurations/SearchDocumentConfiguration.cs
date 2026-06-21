using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> builder)
    {
        builder.ToTable("SearchDocuments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ObjectType).IsRequired().HasMaxLength(40);
        builder.Property(e => e.TargetUrl).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Subtitle).HasMaxLength(1000);
        builder.Property(e => e.Status).HasMaxLength(60);
        builder.Property(e => e.OwnerDisplayName).HasMaxLength(200);
        builder.Property(e => e.LineOfBusiness).HasMaxLength(120);
        builder.Property(e => e.Region).HasMaxLength(120);
        builder.Property(e => e.SearchText).IsRequired();
        builder.Property(e => e.MatchedFieldHintsJson).IsRequired().HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        builder.Property(e => e.LastProjectionError).HasMaxLength(2000);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        // One projection row per source object.
        builder.HasIndex(e => new { e.ObjectType, e.ObjectId })
            .IsUnique()
            .HasDatabaseName("IX_SearchDocuments_Object_Unique");

        // Trigram GIN index for permission-filtered ILIKE search (matches AccountConfiguration convention).
        builder.HasIndex(e => e.SearchText)
            .HasDatabaseName("IX_SearchDocuments_SearchText_Trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Filter indexes for facet/source-visibility predicates.
        builder.HasIndex(e => e.OwnerUserId).HasDatabaseName("IX_SearchDocuments_OwnerUserId");
        builder.HasIndex(e => e.AccountId).HasDatabaseName("IX_SearchDocuments_AccountId");
        builder.HasIndex(e => e.BrokerId).HasDatabaseName("IX_SearchDocuments_BrokerId");
        builder.HasIndex(e => e.Region).HasDatabaseName("IX_SearchDocuments_Region");
        builder.HasIndex(e => e.LineOfBusiness).HasDatabaseName("IX_SearchDocuments_LineOfBusiness");
        builder.HasIndex(e => e.ProgramId).HasDatabaseName("IX_SearchDocuments_ProgramId");
        builder.HasIndex(e => e.TerritoryId).HasDatabaseName("IX_SearchDocuments_TerritoryId");
    }
}
