using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class SavedViewAuditEventConfiguration : IEntityTypeConfiguration<SavedViewAuditEvent>
{
    public void Configure(EntityTypeBuilder<SavedViewAuditEvent> builder)
    {
        builder.ToTable("SavedViewAuditEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).IsRequired().HasMaxLength(40);
        builder.Property(e => e.BeforeJson).HasColumnType("jsonb");
        builder.Property(e => e.AfterJson).HasColumnType("jsonb");

        builder.HasOne(e => e.SavedView)
            .WithMany()
            .HasForeignKey(e => e.SavedViewId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SavedViewId, e.OccurredAt })
            .HasDatabaseName("IX_SavedViewAuditEvents_SavedViewId_OccurredAt");
    }
}
