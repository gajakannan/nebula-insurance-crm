namespace Nebula.Domain.Entities;

public class Broker : BaseEntity
{
    public string LegalName { get; set; } = default!;
    public string LicenseNumber { get; set; } = default!;
    public string State { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    /// <summary>
    /// Stable IdP-issued tenant identity for BrokerUser scope resolution (F0009-S0004 §6).
    /// Links the broker_tenant_id JWT claim to exactly one Broker entity.
    /// NULL for brokers not linked to an external portal user.
    /// </summary>
    public string? BrokerTenantId { get; set; }
    public Guid? ManagedByUserId { get; set; }
    public Guid? MgaId { get; set; }
    public Guid? PrimaryProgramId { get; set; }

    public MGA? Mga { get; set; }
    public Program? PrimaryProgram { get; set; }
    public ICollection<BrokerRegion> BrokerRegions { get; set; } = [];
    public ICollection<Contact> Contacts { get; set; } = [];
}
