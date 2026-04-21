namespace Nebula.Domain.Entities;

public class WorkflowSlaThreshold
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string? LineOfBusiness { get; set; }
    public int WarningDays { get; set; }
    public int TargetDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
