using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ReferenceTaskStatusConfiguration : IEntityTypeConfiguration<ReferenceTaskStatus>
{
    public void Configure(EntityTypeBuilder<ReferenceTaskStatus> builder)
    {
        builder.ToTable("ReferenceTaskStatuses");

        builder.HasKey(e => e.Code);

        builder.Property(e => e.Code).HasMaxLength(30);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(50);
        builder.Property(e => e.DisplayOrder).IsRequired();

        builder.HasData(
            new ReferenceTaskStatus { Code = "Open", DisplayName = "Open", DisplayOrder = 1 },
            new ReferenceTaskStatus { Code = "InProgress", DisplayName = "In Progress", DisplayOrder = 2 },
            new ReferenceTaskStatus { Code = "Done", DisplayName = "Done", DisplayOrder = 3 }
        );
    }
}
