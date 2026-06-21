using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class SavedViewConfiguration : IEntityTypeConfiguration<SavedView>
{
    public void Configure(EntityTypeBuilder<SavedView> builder)
    {
        builder.ToTable("SavedViews");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NormalizedName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(1000);
        builder.Property(e => e.ViewType).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Visibility).IsRequired().HasMaxLength(20);
        builder.Property(e => e.TeamScopeType).HasMaxLength(40);
        builder.Property(e => e.TeamScopeKey).HasMaxLength(200);
        builder.Property(e => e.CriteriaJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.SortJson).IsRequired().HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.IsDefault).HasDefaultValue(false);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        // Duplicate-name protection (409 saved_view_duplicate_name) for active personal views per owner+type.
        builder.HasIndex(e => new { e.OwnerUserId, e.ViewType, e.NormalizedName })
            .HasDatabaseName("IX_SavedViews_Personal_Name")
            .IsUnique()
            .HasFilter("\"Visibility\" = 'Personal' AND \"ArchivedAt\" IS NULL AND \"IsDeleted\" = false");

        // Duplicate-name protection for active team views per scope+type.
        builder.HasIndex(e => new { e.TeamScopeType, e.TeamScopeKey, e.ViewType, e.NormalizedName })
            .HasDatabaseName("IX_SavedViews_Team_Name")
            .IsUnique()
            .HasFilter("\"Visibility\" = 'Team' AND \"ArchivedAt\" IS NULL AND \"IsDeleted\" = false");

        builder.HasIndex(e => new { e.Visibility, e.ViewType }).HasDatabaseName("IX_SavedViews_Visibility_ViewType");
        builder.HasIndex(e => new { e.TeamScopeType, e.TeamScopeKey }).HasDatabaseName("IX_SavedViews_TeamScope");
        builder.HasIndex(e => e.OwnerUserId).HasDatabaseName("IX_SavedViews_OwnerUserId");
    }
}
