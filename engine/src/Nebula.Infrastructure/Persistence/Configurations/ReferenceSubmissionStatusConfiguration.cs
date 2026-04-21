using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;
using Nebula.Domain.Workflow;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ReferenceSubmissionStatusConfiguration : IEntityTypeConfiguration<ReferenceSubmissionStatus>
{
    public void Configure(EntityTypeBuilder<ReferenceSubmissionStatus> builder)
    {
        builder.ToTable("ReferenceSubmissionStatuses");

        builder.HasKey(e => e.Code);

        builder.Property(e => e.Code).HasMaxLength(30);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Description).IsRequired().HasMaxLength(255);
        builder.Property(e => e.IsTerminal).IsRequired();
        builder.Property(e => e.DisplayOrder).IsRequired();
        builder.Property(e => e.ColorGroup).HasMaxLength(20);

        builder.HasIndex(e => e.DisplayOrder).IsUnique();

        builder.HasData(OpportunityStatusCatalog.SubmissionStatuses.Select(s => new ReferenceSubmissionStatus
        {
            Code = s.Code,
            DisplayName = s.DisplayName,
            Description = s.Description,
            IsTerminal = s.IsTerminal,
            DisplayOrder = s.DisplayOrder,
            ColorGroup = s.ColorGroup,
        }));
    }
}
