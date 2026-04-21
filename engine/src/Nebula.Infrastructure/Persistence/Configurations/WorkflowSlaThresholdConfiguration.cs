using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class WorkflowSlaThresholdConfiguration : IEntityTypeConfiguration<WorkflowSlaThreshold>
{
    public void Configure(EntityTypeBuilder<WorkflowSlaThreshold> builder)
    {
        builder.ToTable("WorkflowSlaThresholds", t =>
        {
            t.HasCheckConstraint("CK_WorkflowSlaThresholds_WarningLessThanTarget", "\"WarningDays\" < \"TargetDays\"");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.WarningDays).IsRequired();
        builder.Property(e => e.TargetDays).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => new { e.EntityType, e.Status, e.LineOfBusiness })
            .IsUnique()
            .HasDatabaseName("UX_WorkflowSlaThresholds_EntityType_Status_LineOfBusiness");

        builder.HasData(BuildSeedData());
    }

    private static IReadOnlyList<WorkflowSlaThreshold> BuildSeedData()
    {
        var now = new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc);

        return
        [
            // Submission thresholds
            Seed("1fef8cb4-2f9b-41e8-9329-3c1a5d22790a", "submission", "Received", null, 1, 2, now),
            Seed("f6969f8b-7ab9-4ab0-a6e9-26f95f57b21c", "submission", "Triaging", null, 1, 2, now),
            Seed("379f3ad6-68f0-4d2f-b52f-5ab9bb40f157", "submission", "WaitingOnBroker", null, 2, 3, now),

            // Renewal thresholds
            Seed("30efe68f-9e5c-4e7f-9191-e68ee0f8eb26", "renewal", "Identified", null, 60, 90, now),
            Seed("1e92d4d0-b89a-4b5e-9e01-7d4cf14ed564", "renewal", "Identified", "Property", 60, 90, now),
            Seed("c47d2142-e4b2-4dc3-90c8-3f0da6a07f8b", "renewal", "Identified", "GeneralLiability", 60, 90, now),
            Seed("d7286c4c-38d5-4e57-9837-2b44cf2a86cf", "renewal", "Identified", "WorkersCompensation", 90, 120, now),
            Seed("0ebb7f8c-9709-4b54-a6a4-dcff0b2d3de5", "renewal", "Identified", "ProfessionalLiability", 60, 90, now),
            Seed("d5bc3dd5-17ec-4f56-a8c6-f5b503f17f0d", "renewal", "Identified", "Cyber", 45, 60, now),
            Seed("bb695667-05cf-43dd-a89c-c05e4747967c", "renewal", "Outreach", null, 3, 7, now),
            Seed("77ca3fa9-fddd-47ec-b4d2-84bcbf001687", "renewal", "InReview", null, 5, 14, now),
            Seed("f501f5dd-23d4-4250-9eab-65a70d0c08f5", "renewal", "Quoted", null, 7, 21, now),
        ];
    }

    private static WorkflowSlaThreshold Seed(
        string id,
        string entityType,
        string status,
        string? lineOfBusiness,
        int warningDays,
        int targetDays,
        DateTime timestampUtc) => new()
        {
            Id = Guid.Parse(id),
            EntityType = entityType,
            Status = status,
            LineOfBusiness = lineOfBusiness,
            WarningDays = warningDays,
            TargetDays = targetDays,
            CreatedAt = timestampUtc,
            UpdatedAt = timestampUtc,
        };
}
