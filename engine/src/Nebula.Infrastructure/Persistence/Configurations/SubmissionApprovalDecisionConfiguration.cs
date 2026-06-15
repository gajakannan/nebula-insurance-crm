using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class SubmissionApprovalDecisionConfiguration : IEntityTypeConfiguration<SubmissionApprovalDecision>
{
    public void Configure(EntityTypeBuilder<SubmissionApprovalDecision> builder)
    {
        builder.ToTable("SubmissionApprovalDecisions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Decision).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.AuthorityContextJson).IsRequired().HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb");
        builder.Property(e => e.BlockingConditionsJson).IsRequired().HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Submission)
            .WithMany(e => e.ApprovalDecisions)
            .HasForeignKey(e => e.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.SubmissionId, e.DecidedAt })
            .HasDatabaseName("IX_SubmissionApprovalDecisions_SubmissionId_DecidedAt");
    }
}
