using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ActivityTimelineEventConfiguration : IEntityTypeConfiguration<ActivityTimelineEvent>
{
    public void Configure(EntityTypeBuilder<ActivityTimelineEvent> builder)
    {
        builder.ToTable("ActivityTimelineEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EventType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EventPayloadJson).HasColumnType("jsonb");
        builder.Property(e => e.EventDescription).IsRequired().HasMaxLength(500);
        builder.Property(e => e.BrokerDescription).HasMaxLength(500);
        builder.Property(e => e.ActorUserId).IsRequired();
        builder.Property(e => e.ActorDisplayName).HasMaxLength(200);
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => new { e.EntityType, e.OccurredAt })
            .HasDatabaseName("IX_ATE_EntityType_OccurredAt")
            .IsDescending(false, true);
    }
}
