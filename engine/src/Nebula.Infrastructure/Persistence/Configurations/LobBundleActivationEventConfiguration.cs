using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class LobBundleActivationEventConfiguration : IEntityTypeConfiguration<LobBundleActivationEvent>
{
    public void Configure(EntityTypeBuilder<LobBundleActivationEvent> builder)
    {
        builder.ToTable("LobBundleActivationEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FromStatus).IsRequired().HasMaxLength(30);
        builder.Property(e => e.ToStatus).IsRequired().HasMaxLength(30);
        builder.Property(e => e.ChangeNote).HasMaxLength(1000);
        builder.Property(e => e.ActorUserId).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.LobSchemaBundle)
            .WithMany(e => e.ActivationEvents)
            .HasForeignKey(e => e.LobSchemaBundleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => new { e.LobSchemaBundleId, e.OccurredAt })
            .HasDatabaseName("IX_LobBundleActivationEvents_Bundle_OccurredAt");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
