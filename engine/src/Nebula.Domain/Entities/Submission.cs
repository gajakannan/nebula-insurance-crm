namespace Nebula.Domain.Entities;

public class Submission : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid BrokerId { get; set; }
    public Guid? ProgramId { get; set; }
    public string? LineOfBusiness { get; set; }
    public string CurrentStatus { get; set; } = "Received";
    public DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal? PremiumEstimate { get; set; }
    public string? Description { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string AccountDisplayNameAtLink { get; set; } = default!;
    public string AccountStatusAtRead { get; set; } = default!;
    public Guid? AccountSurvivorId { get; set; }

    public Account Account { get; set; } = default!;
    public Broker Broker { get; set; } = default!;
    public Program? Program { get; set; }
    public UserProfile AssignedToUser { get; set; } = default!;
}
