using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class RenewalConfiguration : IEntityTypeConfiguration<Renewal>
{
    public void Configure(EntityTypeBuilder<Renewal> builder)
    {
        builder.ToTable("Renewals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.CurrentStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Identified");
        builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.PolicyExpirationDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.TargetOutreachDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.AssignedToUserId).IsRequired();
        builder.Property(e => e.LostReasonCode).HasMaxLength(50);
        builder.Property(e => e.LostReasonDetail).HasMaxLength(500);
        builder.Property(e => e.AccountDisplayNameAtLink).IsRequired().HasMaxLength(200);
        builder.Property(e => e.AccountStatusAtRead).IsRequired().HasMaxLength(20);
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

        builder.HasOne(e => e.Policy)
            .WithMany()
            .HasForeignKey(e => e.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.BoundPolicy)
            .WithMany()
            .HasForeignKey(e => e.BoundPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedToUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RenewalSubmission)
            .WithMany()
            .HasForeignKey(e => e.RenewalSubmissionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.CurrentStatus)
            .HasDatabaseName("IX_Renewals_CurrentStatus");

        builder.HasIndex(e => new { e.AssignedToUserId, e.CurrentStatus })
            .HasDatabaseName("IX_Renewals_AssignedToUserId_CurrentStatus");

        builder.HasIndex(e => new { e.PolicyExpirationDate, e.CurrentStatus })
            .HasDatabaseName("IX_Renewals_PolicyExpirationDate_CurrentStatus");

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Renewals_AccountId");

        builder.HasIndex(e => e.BrokerId)
            .HasDatabaseName("IX_Renewals_BrokerId");

        builder.HasIndex(e => e.PolicyId)
            .HasDatabaseName("IX_Renewals_PolicyId_Active")
            .HasFilter("\"IsDeleted\" = false AND \"CurrentStatus\" NOT IN ('Completed', 'Lost')")
            .IsUnique();

        builder.HasIndex(e => e.TargetOutreachDate)
            .HasDatabaseName("IX_Renewals_TargetOutreachDate")
            .HasFilter("\"IsDeleted\" = false AND \"CurrentStatus\" = 'Identified'");
    }
}
