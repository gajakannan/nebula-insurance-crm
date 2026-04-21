using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .HasColumnName("DisplayName")
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(e => e.LegalName).HasMaxLength(200);
        builder.Property(e => e.TaxId).HasMaxLength(50);
        builder.Property(e => e.Industry).HasMaxLength(100);
        builder.Property(e => e.PrimaryLineOfBusiness).HasMaxLength(50);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.TerritoryCode).HasMaxLength(50);
        builder.Property(e => e.Region).HasMaxLength(50);
        builder.Property(e => e.PrimaryState)
            .HasColumnName("State")
            .HasMaxLength(50);
        builder.Property(e => e.Address1).HasMaxLength(200);
        builder.Property(e => e.Address2).HasMaxLength(200);
        builder.Property(e => e.City).HasMaxLength(100);
        builder.Property(e => e.PostalCode).HasMaxLength(20);
        builder.Property(e => e.Country).HasMaxLength(50);
        builder.Property(e => e.StableDisplayName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.DeleteReasonCode).HasMaxLength(50);
        builder.Property(e => e.DeleteReasonDetail).HasMaxLength(500);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.BrokerOfRecord)
            .WithMany()
            .HasForeignKey(e => e.BrokerOfRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PrimaryProducer)
            .WithMany()
            .HasForeignKey(e => e.PrimaryProducerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.MergedInto)
            .WithMany(e => e.MergedAccounts)
            .HasForeignKey(e => e.MergedIntoAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Accounts_Status");

        builder.HasIndex(e => new { e.Status, e.Region })
            .HasDatabaseName("IX_Accounts_Status_Region");

        builder.HasIndex(e => e.BrokerOfRecordId)
            .HasDatabaseName("IX_Accounts_BrokerOfRecordId");

        builder.HasIndex(e => e.TerritoryCode)
            .HasDatabaseName("IX_Accounts_TerritoryCode");

        builder.HasIndex(e => e.MergedIntoAccountId)
            .HasDatabaseName("IX_Accounts_MergedIntoAccountId");

        builder.HasIndex(e => e.TaxId)
            .HasDatabaseName("IX_Accounts_TaxId_Active")
            .HasFilter("\"Status\" = 'Active' AND \"TaxId\" IS NOT NULL AND \"IsDeleted\" = false")
            .IsUnique();

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_Accounts_DisplayName_Trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}
