using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ConfigurationAuditEventConfiguration : IEntityTypeConfiguration<ConfigurationAuditEvent>
{
    public void Configure(EntityTypeBuilder<ConfigurationAuditEvent> builder)
    {
        builder.ToTable("ConfigurationAuditEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.DomainKey).IsRequired().HasMaxLength(80);
        builder.Property(e => e.Action).IsRequired().HasMaxLength(60);
        builder.Property(e => e.Outcome).IsRequired().HasMaxLength(30);
        builder.Property(e => e.SummaryJson).IsRequired().HasColumnType("jsonb");
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasOne(e => e.Domain).WithMany(e => e.AuditEvents).HasForeignKey(e => e.DomainKey).HasPrincipalKey(e => e.DomainKey).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => new { e.DomainKey, e.CreatedAt }).HasDatabaseName("IX_ConfigurationAuditEvents_Domain_CreatedAt");
        builder.HasIndex(e => new { e.Action, e.Outcome }).HasDatabaseName("IX_ConfigurationAuditEvents_Action_Outcome");
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
