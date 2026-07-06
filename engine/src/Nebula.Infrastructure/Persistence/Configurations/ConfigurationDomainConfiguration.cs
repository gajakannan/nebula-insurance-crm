using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nebula.Domain.Entities;

namespace Nebula.Infrastructure.Persistence.Configurations;

public class ConfigurationDomainConfiguration : IEntityTypeConfiguration<ConfigurationDomain>
{
    public void Configure(EntityTypeBuilder<ConfigurationDomain> builder)
    {
        builder.ToTable("ConfigurationDomains");
        builder.HasKey(e => e.DomainKey);
        builder.Property(e => e.Id).IsRequired();
        builder.Property(e => e.DomainKey).HasMaxLength(80).ValueGeneratedNever();
        builder.Property(e => e.DisplayName).IsRequired().HasMaxLength(160);
        builder.Property(e => e.OwningModule).IsRequired().HasMaxLength(20);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(30);
        builder.Property(e => e.EditableSchemaRef).IsRequired().HasMaxLength(240);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);
        builder.Property(e => e.RowVersion).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
        builder.HasIndex(e => e.OwningModule);
        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.HasData(
            Seed("7c4b1546-f20f-4645-91c1-a2c9d85e4c99", "queue-routing", "Queue and Routing", "F0022", "planning-mds/schemas/admin-configuration-domain.schema.json", true),
            Seed("dfd4904c-cfae-440f-9af2-8329888ca987", "workflow-sla-thresholds", "Workflow SLA Thresholds", "F0032", "planning-mds/schemas/admin-configuration-draft.schema.json", true),
            Seed("5fd8c5b6-e56d-402c-95a2-a101712c1638", "search-report-defaults", "Search and Report Defaults", "F0023", "planning-mds/schemas/admin-configuration-draft.schema.json", true),
            Seed("7db99f11-312b-491e-b9ad-8c0e31a9c737", "template-metadata", "Template Metadata", "F0027", "planning-mds/schemas/admin-configuration-draft.schema.json", true));
    }

    private static ConfigurationDomain Seed(string id, string key, string name, string module, string schemaRef, bool rollback)
    {
        var now = new DateTime(2026, 7, 6, 0, 0, 0, DateTimeKind.Utc);
        return new ConfigurationDomain
        {
            Id = Guid.Parse(id),
            DomainKey = key,
            DisplayName = name,
            OwningModule = module,
            Status = "Supported",
            EditableSchemaRef = schemaRef,
            SupportsRollback = rollback,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}
