using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ServiceCaseConfiguration : IEntityTypeConfiguration<ServiceCase>
{
    public void Configure(EntityTypeBuilder<ServiceCase> builder)
    {
        builder.ToTable("ServiceCases");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.CaseNumber).IsRequired().HasMaxLength(32);
        builder.Property(e => e.Summary).IsRequired().HasMaxLength(500);
        builder.Property(e => e.Description).HasMaxLength(8000);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(40).HasDefaultValue("Intake");
        builder.Property(e => e.Priority).IsRequired().HasMaxLength(40).HasDefaultValue("Medium");
        builder.Property(e => e.FollowUpSummary).HasMaxLength(1000);
        builder.Property(e => e.ResolutionSummary).HasMaxLength(2000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.Policy)
            .WithMany()
            .HasForeignKey(e => e.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.CaseNumber).IsUnique().HasDatabaseName("UX_ServiceCases_CaseNumber");
        builder.HasIndex(e => new { e.AccountId, e.Status }).HasDatabaseName("IX_ServiceCases_Account_Status");
        builder.HasIndex(e => new { e.PolicyId, e.Status }).HasDatabaseName("IX_ServiceCases_Policy_Status");
        builder.HasIndex(e => new { e.OwnerUserId, e.Status, e.DueDate }).HasDatabaseName("IX_ServiceCases_Owner_Status_DueDate");
    }
}
