using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(40);
        builder.Property(e => e.AccountId).IsRequired();
        builder.Property(e => e.BrokerId).IsRequired();
        builder.Property(e => e.CarrierId).IsRequired();
        builder.Property(e => e.LineOfBusiness).IsRequired().HasMaxLength(50);
        builder.Property(e => e.EffectiveDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.ExpirationDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.TotalPremium).HasColumnName("Premium").HasColumnType("decimal(18,2)");
        builder.Property(e => e.PremiumCurrency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
        builder.Property(e => e.CurrentStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Pending");
        builder.Property(e => e.BoundAt);
        builder.Property(e => e.IssuedAt);
        builder.Property(e => e.CancelledAt);
        builder.Property(e => e.CancellationEffectiveDate).HasColumnType("date");
        builder.Property(e => e.CancellationReasonCode).HasMaxLength(60);
        builder.Property(e => e.CancellationReasonDetail).HasMaxLength(500);
        builder.Property(e => e.ReinstatementDeadline).HasColumnType("date");
        builder.Property(e => e.ExpiredAt);
        builder.Property(e => e.ImportSource).IsRequired().HasMaxLength(40).HasDefaultValue("manual");
        builder.Property(e => e.ExternalPolicyReference).HasMaxLength(100);
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

        builder.HasOne(e => e.Carrier)
            .WithMany()
            .HasForeignKey(e => e.CarrierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Producer)
            .WithMany()
            .HasForeignKey(e => e.ProducerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.PredecessorPolicy)
            .WithMany()
            .HasForeignKey(e => e.PredecessorPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasIndex(e => e.PolicyNumber)
            .HasDatabaseName("IX_Policies_PolicyNumber")
            .IsUnique();

        builder.HasIndex(e => e.ExpirationDate)
            .HasDatabaseName("IX_Policies_ExpirationDate");

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_Policies_AccountId");

        builder.HasIndex(e => e.BrokerId)
            .HasDatabaseName("IX_Policies_BrokerId");

        builder.HasIndex(e => e.CarrierId)
            .HasDatabaseName("IX_Policies_CarrierId");

        builder.HasIndex(e => e.CurrentStatus)
            .HasDatabaseName("IX_Policies_CurrentStatus");

        builder.HasIndex(e => e.CurrentVersionId)
            .HasDatabaseName("IX_Policies_CurrentVersionId");

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
