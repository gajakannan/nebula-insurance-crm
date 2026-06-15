using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class SubmissionBindHandoffConfiguration : IEntityTypeConfiguration<SubmissionBindHandoff>
{
    public void Configure(EntityTypeBuilder<SubmissionBindHandoff> builder)
    {
        builder.ToTable("SubmissionBindHandoffs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(120);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
        builder.Property(e => e.PayloadSnapshotJson).IsRequired().HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Submission)
            .WithMany(e => e.BindHandoffs)
            .HasForeignKey(e => e.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.SubmissionId, e.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("IX_SubmissionBindHandoffs_SubmissionId_IdempotencyKey");
    }
}
