using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class AccountContactConfiguration : IEntityTypeConfiguration<AccountContact>
{
    public void Configure(EntityTypeBuilder<AccountContact> builder)
    {
        builder.ToTable("AccountContacts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Role).HasMaxLength(100);
        builder.Property(e => e.Email).HasMaxLength(200);
        builder.Property(e => e.Phone).HasMaxLength(50);
        builder.Property(e => e.IsPrimary).HasDefaultValue(false);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Account)
            .WithMany(account => account.AccountContacts)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_AccountContacts_AccountId");

        builder.HasIndex(e => e.AccountId)
            .HasDatabaseName("IX_AccountContacts_AccountId_Primary")
            .HasFilter("\"IsPrimary\" = true AND \"IsDeleted\" = false")
            .IsUnique();
    }
}
