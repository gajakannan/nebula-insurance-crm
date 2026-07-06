using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class TerritoryAssignmentConfiguration : IEntityTypeConfiguration<TerritoryAssignment>
{
    public void Configure(EntityTypeBuilder<TerritoryAssignment> builder)
    {
        builder.ToTable("TerritoryAssignments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.TerritoryId).IsRequired();
        builder.Property(e => e.MemberType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.MemberId).IsRequired();
        builder.Property(e => e.EffectiveFrom).IsRequired();
        builder.Property(e => e.EffectiveTo);
        builder.Property(e => e.AssignmentReason).HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        // FK to Territory configured from the Territory side (Territory.Assignments).

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.TerritoryId).HasDatabaseName("IX_TerritoryAssignments_TerritoryId");
        builder.HasIndex(e => new { e.MemberType, e.MemberId, e.EffectiveFrom })
            .HasDatabaseName("IX_TerritoryAssignments_Member_AsOf");
        // Single-open-member-assignment filtered unique index added via raw SQL in the F0017 migration.
    }
}
