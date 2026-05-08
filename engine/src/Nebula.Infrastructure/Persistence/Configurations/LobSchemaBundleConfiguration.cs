using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class LobSchemaBundleConfiguration : IEntityTypeConfiguration<LobSchemaBundle>
{
    public void Configure(EntityTypeBuilder<LobSchemaBundle> builder)
    {
        builder.ToTable("LobSchemaBundles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.SchemaVersion).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Draft");
        builder.Property(e => e.DataSchemaJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.UiSchemaJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.RulesJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.ProjectionMapJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.ContentHash).IsRequired().HasMaxLength(128);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.LobProductVersion)
            .WithMany(e => e.SchemaBundles)
            .HasForeignKey(e => e.LobProductVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.LobProductVersionId, e.SchemaVersion })
            .IsUnique()
            .HasDatabaseName("UX_LobSchemaBundles_ProductVersion_SchemaVersion");

        builder.HasIndex(e => new { e.LobProductVersionId, e.Status })
            .HasDatabaseName("IX_LobSchemaBundles_ProductVersion_Status");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
