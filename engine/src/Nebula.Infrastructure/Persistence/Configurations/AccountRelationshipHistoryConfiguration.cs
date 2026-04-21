using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class AccountRelationshipHistoryConfiguration : IEntityTypeConfiguration<AccountRelationshipHistory>
{
    public void Configure(EntityTypeBuilder<AccountRelationshipHistory> builder)
    {
        builder.ToTable("AccountRelationshipHistory");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RelationshipType).IsRequired().HasMaxLength(30);
        builder.Property(e => e.PreviousValue).HasMaxLength(200);
        builder.Property(e => e.NewValue).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(500);
        builder.Property(e => e.EffectiveAt).IsRequired();
        builder.Property(e => e.ActorUserId).IsRequired();

        builder.HasOne(e => e.Account)
            .WithMany(account => account.RelationshipHistory)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.AccountId, e.EffectiveAt })
            .HasDatabaseName("IX_AccountRelationshipHistory_AccountId_EffectiveAt")
            .IsDescending(false, true);
    }
}
