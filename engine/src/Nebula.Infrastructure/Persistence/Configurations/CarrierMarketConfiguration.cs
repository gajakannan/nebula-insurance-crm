using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class CarrierMarketConfiguration : IEntityTypeConfiguration<CarrierMarket>
{
    public void Configure(EntityTypeBuilder<CarrierMarket> builder)
    {
        builder.ToTable("CarrierMarkets");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Code).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.NaicCode).HasMaxLength(10);
        builder.Property(e => e.AmBestRating).HasMaxLength(10);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.MarketType).IsRequired().HasMaxLength(40);
        builder.Property(e => e.WebsiteUrl).HasMaxLength(500);
        builder.Property(e => e.GeneralEmail).HasMaxLength(320);
        builder.Property(e => e.MainPhone).HasMaxLength(30);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(e => e.Code).IsUnique().HasDatabaseName("IX_CarrierMarkets_Code");
        builder.HasIndex(e => new { e.Status, e.MarketType }).HasDatabaseName("IX_CarrierMarkets_Status_MarketType");

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class CarrierMarketContactConfiguration : IEntityTypeConfiguration<CarrierMarketContact>
{
    public void Configure(EntityTypeBuilder<CarrierMarketContact> builder)
    {
        builder.ToTable("CarrierMarketContacts");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FullName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Title).HasMaxLength(120);
        builder.Property(e => e.Email).HasMaxLength(320);
        builder.Property(e => e.Phone).HasMaxLength(30);
        builder.Property(e => e.Roles).IsRequired().HasColumnType("text[]");
        builder.Property(e => e.Notes).HasMaxLength(1000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.CarrierMarket)
            .WithMany(e => e.Contacts)
            .HasForeignKey(e => e.CarrierMarketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CarrierMarketId, e.IsPrimary })
            .HasDatabaseName("IX_CarrierMarketContacts_Market_Primary");

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class CarrierAppetiteNoteConfiguration : IEntityTypeConfiguration<CarrierAppetiteNote>
{
    public void Configure(EntityTypeBuilder<CarrierAppetiteNote> builder)
    {
        builder.ToTable("CarrierAppetiteNotes");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.LineOfBusiness).HasMaxLength(120);
        builder.Property(e => e.Region).HasMaxLength(120);
        builder.Property(e => e.AppetiteLevel).IsRequired().HasMaxLength(40);
        builder.Property(e => e.Summary).IsRequired().HasMaxLength(240);
        builder.Property(e => e.Detail).HasMaxLength(4000);
        builder.Property(e => e.Source).HasMaxLength(200);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.CarrierMarket)
            .WithMany(e => e.AppetiteNotes)
            .HasForeignKey(e => e.CarrierMarketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CarrierMarketId, e.LineOfBusiness, e.Region })
            .HasDatabaseName("IX_CarrierAppetiteNotes_Market_Lob_Region");

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class CarrierAppointmentConfiguration : IEntityTypeConfiguration<CarrierAppointment>
{
    public void Configure(EntityTypeBuilder<CarrierAppointment> builder)
    {
        builder.ToTable("CarrierAppointments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.AppointmentStatus).IsRequired().HasMaxLength(40);
        builder.Property(e => e.States).IsRequired().HasColumnType("text[]");
        builder.Property(e => e.LineOfBusiness).HasMaxLength(120);
        builder.Property(e => e.AppointmentNumber).HasMaxLength(80);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasOne(e => e.CarrierMarket)
            .WithMany(e => e.Appointments)
            .HasForeignKey(e => e.CarrierMarketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CarrierMarketId, e.AppointmentStatus })
            .HasDatabaseName("IX_CarrierAppointments_Market_Status");

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}

public class CarrierMarketActivityLinkConfiguration : IEntityTypeConfiguration<CarrierMarketActivityLink>
{
    public void Configure(EntityTypeBuilder<CarrierMarketActivityLink> builder)
    {
        builder.ToTable("CarrierMarketActivityLinks");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RelatedEntityType).IsRequired().HasMaxLength(40);
        builder.Property(e => e.RelationshipKind).IsRequired().HasMaxLength(60);
        builder.Property(e => e.Note).HasMaxLength(1000);
        builder.Property(e => e.CreatedByUserId).IsRequired();

        builder.HasOne(e => e.CarrierMarket)
            .WithMany(e => e.ActivityLinks)
            .HasForeignKey(e => e.CarrierMarketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CarrierMarketId, e.RelatedEntityType, e.RelatedEntityId })
            .HasDatabaseName("IX_CarrierMarketActivityLinks_Market_Related");
    }
}
