using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class BrokerConfiguration : IEntityTypeConfiguration<Broker>
{
    public void Configure(EntityTypeBuilder<Broker> builder)
    {
        builder.ToTable("Brokers");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.LicenseNumber).IsRequired().HasMaxLength(50);
        builder.Property(e => e.State).IsRequired().HasMaxLength(2);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Email).HasMaxLength(255);
        builder.Property(e => e.Phone).HasMaxLength(30);
        builder.Property(e => e.BrokerTenantId).HasMaxLength(200);
        builder.Property(e => e.ManagedByUserId);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.Mga)
            .WithMany()
            .HasForeignKey(e => e.MgaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.PrimaryProgram)
            .WithMany()
            .HasForeignKey(e => e.PrimaryProgramId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => e.LicenseNumber).IsUnique()
            .HasDatabaseName("IX_Brokers_LicenseNumber");
        builder.HasIndex(e => e.BrokerTenantId)
            .HasDatabaseName("IX_Brokers_BrokerTenantId")
            .IsUnique()
            .HasFilter("\"BrokerTenantId\" IS NOT NULL");
        builder.HasIndex(e => e.ManagedByUserId)
            .HasDatabaseName("IX_Brokers_ManagedByUserId");
        builder.HasIndex(e => new { e.Status, e.IsDeleted })
            .HasDatabaseName("IX_Brokers_Status_IsDeleted");
    }
}
