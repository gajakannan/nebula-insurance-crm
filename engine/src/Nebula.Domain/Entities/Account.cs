using System.ComponentModel.DataAnnotations.Schema;

namespace Nebula.Domain.Entities;

public class Account : BaseEntity
{
    // Legacy property retained for internal compatibility. Maps to the
    // renamed DisplayName column so existing query code keeps working.
    public string Name { get; set; } = default!;
    public string? LegalName { get; set; }
    public string? TaxId { get; set; }
    public string? Industry { get; set; }
    public string? PrimaryLineOfBusiness { get; set; }
    public string Status { get; set; } = AccountStatuses.Active;
    public Guid? BrokerOfRecordId { get; set; }
    public Guid? PrimaryProducerUserId { get; set; }
    public string? TerritoryCode { get; set; }
    public string? Region { get; set; }
    // Legacy property retained for internal compatibility. Maps to the renamed
    // State column so current renewal/submission code does not need a full rename.
    public string PrimaryState { get; set; } = default!;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public string StableDisplayName { get; set; } = default!;
    public Guid? MergedIntoAccountId { get; set; }
    public string? DeleteReasonCode { get; set; }
    public string? DeleteReasonDetail { get; set; }
    public DateTime? RemovedAt { get; set; }

    public Broker? BrokerOfRecord { get; set; }
    public UserProfile? PrimaryProducer { get; set; }
    public Account? MergedInto { get; set; }
    public ICollection<Account> MergedAccounts { get; } = new List<Account>();
    public ICollection<AccountContact> AccountContacts { get; } = new List<AccountContact>();
    public ICollection<AccountRelationshipHistory> RelationshipHistory { get; } = new List<AccountRelationshipHistory>();

    [NotMapped]
    public string DisplayName
    {
        get => Name;
        set => Name = value;
    }

    [NotMapped]
    public string State
    {
        get => PrimaryState;
        set => PrimaryState = value;
    }

    [NotMapped]
    public Guid? SurvivorAccountId => MergedIntoAccountId;
}

public static class AccountStatuses
{
    public const string Active = "Active";
    public const string Inactive = "Inactive";
    public const string Merged = "Merged";
    public const string Deleted = "Deleted";
}
