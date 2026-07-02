using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CommunicationEventConfiguration : IEntityTypeConfiguration<CommunicationEvent>
{
    public void Configure(EntityTypeBuilder<CommunicationEvent> builder)
    {
        builder.ToTable("CommunicationEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Type).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Direction).HasMaxLength(40);
        builder.Property(e => e.Summary).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Body).HasMaxLength(8000);
        builder.Property(e => e.Visibility).IsRequired().HasMaxLength(40).HasDefaultValue("InternalOnly");
        builder.Property(e => e.EmailProvider).HasMaxLength(100);
        builder.Property(e => e.EmailMessageId).HasMaxLength(300);
        builder.Property(e => e.EmailSubject).HasMaxLength(500);
        builder.Property(e => e.RedactionReason).HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasIndex(e => new { e.Type, e.OccurredAt }).HasDatabaseName("IX_CommunicationEvents_Type_OccurredAt");
        builder.HasIndex(e => e.EmailMessageId).HasDatabaseName("IX_CommunicationEvents_EmailMessageId");
    }
}
