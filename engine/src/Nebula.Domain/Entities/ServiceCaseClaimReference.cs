namespace Nebula.Domain.Entities;

public class ServiceCaseClaimReference : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public string? CarrierClaimNumber { get; set; }
    public DateOnly? DateOfLoss { get; set; }
    public string? ClaimantDisplayName { get; set; }
    public string? LossSummary { get; set; }
    public string? CarrierContactReference { get; set; }

    public ServiceCase ServiceCase { get; set; } = default!;
}
