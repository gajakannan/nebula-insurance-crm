using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Phone).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Role).IsRequired().HasMaxLength(50);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Broker)
            .WithMany(b => b.Contacts)
            .HasForeignKey(e => e.BrokerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Account)
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
