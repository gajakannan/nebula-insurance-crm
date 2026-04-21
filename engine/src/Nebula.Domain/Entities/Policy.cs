namespace Nebula.Domain.Entities;

public class Policy : BaseEntity
{
    public string PolicyNumber { get; set; } = default!;
    public Guid AccountId { get; set; }
    public Guid BrokerId { get; set; }
    public string? Carrier { get; set; }
    public string? LineOfBusiness { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public decimal? Premium { get; set; }
    public string CurrentStatus { get; set; } = "Active";
    public string AccountDisplayNameAtLink { get; set; } = default!;
    public string AccountStatusAtRead { get; set; } = default!;
    public Guid? AccountSurvivorId { get; set; }

    public Account Account { get; set; } = default!;
    public Broker Broker { get; set; } = default!;
}
