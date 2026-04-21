using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurrentStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Received");
        builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.EffectiveDate).IsRequired();
        builder.Property(e => e.ExpirationDate).HasColumnType("date");
        builder.Property(e => e.PremiumEstimate).HasPrecision(18, 2);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.AccountDisplayNameAtLink).IsRequired().HasMaxLength(200);
        builder.Property(e => e.AccountStatusAtRead).IsRequired().HasMaxLength(20);
        builder.Property(e => e.AssignedToUserId).IsRequired();
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Broker)
            .WithMany()
            .HasForeignKey(e => e.BrokerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Program)
            .WithMany()
            .HasForeignKey(e => e.ProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedToUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.CurrentStatus)
            .HasDatabaseName("IX_Submissions_CurrentStatus");

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Submissions_AccountId");

        builder.HasIndex(e => e.BrokerId)
            .HasDatabaseName("IX_Submissions_BrokerId");

        builder.HasIndex(e => e.EffectiveDate)
            .HasDatabaseName("IX_Submissions_EffectiveDate");

        builder.HasIndex(e => e.AssignedToUserId)
            .HasDatabaseName("IX_Submissions_AssignedToUserId");

        builder.HasIndex(e => new { e.AssignedToUserId, e.CurrentStatus })
            .HasDatabaseName("IX_Submissions_AssignedToUserId_CurrentStatus");
    }
}
