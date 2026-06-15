using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ProducerOwnershipConfiguration : IEntityTypeConfiguration<ProducerOwnership>
{
    public void Configure(EntityTypeBuilder<ProducerOwnership> builder)
    {
        builder.ToTable("ProducerOwnership");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.ScopeType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.ScopeId).IsRequired();
        builder.Property(e => e.ProducerNodeId).IsRequired();
        builder.Property(e => e.EffectiveFrom).IsRequired();
        builder.Property(e => e.EffectiveTo);
        builder.Property(e => e.AssignmentReason).HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.ProducerNode)
            .WithMany()
            .HasForeignKey(e => e.ProducerNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.ScopeType, e.ScopeId }).HasDatabaseName("IX_ProducerOwnership_Scope");
        // Single-open-period filtered unique index added via raw SQL in the F0017 migration.
    }
}
