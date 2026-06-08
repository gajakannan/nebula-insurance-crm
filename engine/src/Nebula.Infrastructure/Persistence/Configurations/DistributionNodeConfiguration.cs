using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class DistributionNodeConfiguration : IEntityTypeConfiguration<DistributionNode>
{
    public void Configure(EntityTypeBuilder<DistributionNode> builder)
    {
        builder.ToTable("DistributionNodes");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.NodeType).IsRequired().HasMaxLength(20);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.AncestryPath).IsRequired().HasColumnType("text").HasDefaultValue("");
        builder.Property(e => e.Depth).HasDefaultValue(0);
        builder.Property(e => e.ChildCount).HasDefaultValue(0);
        builder.Property(e => e.IsActive).HasDefaultValue(true);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.ParentId).HasDatabaseName("IX_DistributionNodes_ParentId");
        builder.HasIndex(e => e.AncestryPath).HasDatabaseName("IX_DistributionNodes_AncestryPath");
        builder.HasIndex(e => new { e.NodeType, e.IsActive }).HasDatabaseName("IX_DistributionNodes_NodeType_IsActive");
    }
}
