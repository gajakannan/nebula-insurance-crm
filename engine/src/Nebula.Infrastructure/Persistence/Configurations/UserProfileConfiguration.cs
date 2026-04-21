using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.IdpIssuer).IsRequired().HasMaxLength(500);
        builder.Property(e => e.IdpSubject).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(255);
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Department).IsRequired().HasMaxLength(100);
        builder.Property(e => e.RegionsJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.RolesJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => new { e.IdpIssuer, e.IdpSubject }).IsUnique()
            .HasDatabaseName("IX_UserProfiles_IdpIssuer_IdpSubject");

        // F0004: Index for user search (assignee picker typeahead)
        builder.HasIndex(e => e.DisplayName)
            .HasDatabaseName("IX_UserProfiles_DisplayName");
    }
}
