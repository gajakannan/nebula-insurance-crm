using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class LobProductVersionConfiguration : IEntityTypeConfiguration<LobProductVersion>
{
    public void Configure(EntityTypeBuilder<LobProductVersion> builder)
    {
        builder.ToTable("LobProductVersions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Version).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Active");
        builder.Property(e => e.EffectiveFrom).IsRequired().HasColumnType("date");
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.LobProduct)
            .WithMany(e => e.Versions)
            .HasForeignKey(e => e.LobProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.LobProductId, e.Version })
            .IsUnique()
            .HasDatabaseName("UX_LobProductVersions_Product_Version");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
