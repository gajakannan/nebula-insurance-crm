using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class MGAConfiguration : IEntityTypeConfiguration<MGA>
{
    public void Configure(EntityTypeBuilder<MGA> builder)
    {
        builder.ToTable("MGAs");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.ExternalCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.CreatedByUserId).IsRequired();
        builder.Property(e => e.UpdatedByUserId).IsRequired();
        builder.Property(e => e.DeletedByUserId);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
