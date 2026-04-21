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

        builder.Property(e => e.PolicyNumber).IsRequired().HasMaxLength(50);
        builder.Property(e => e.AccountId).IsRequired();
        builder.Property(e => e.BrokerId).IsRequired();
        builder.Property(e => e.Carrier).HasMaxLength(100);
        builder.Property(e => e.LineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.EffectiveDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.ExpirationDate).IsRequired().HasColumnType("date");
        builder.Property(e => e.Premium).HasColumnType("decimal(18,2)");
        builder.Property(e => e.CurrentStatus).IsRequired().HasMaxLength(30).HasDefaultValue("Active");
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

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
