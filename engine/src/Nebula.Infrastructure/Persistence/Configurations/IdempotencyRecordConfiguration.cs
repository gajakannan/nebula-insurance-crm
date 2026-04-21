using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(120);
        builder.Property(e => e.Operation).IsRequired().HasMaxLength(60);
        builder.Property(e => e.ActorUserId).IsRequired();
        builder.Property(e => e.ResponseStatusCode).IsRequired();
        builder.Property(e => e.ResponsePayloadJson).HasColumnType("jsonb");
        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.IdempotencyKey, e.Operation })
            .HasDatabaseName("IX_IdempotencyRecords_Key_Operation")
            .IsUnique();
    }
}
