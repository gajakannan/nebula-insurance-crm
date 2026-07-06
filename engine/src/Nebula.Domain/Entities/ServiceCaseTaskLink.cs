namespace Nebula.Domain.Entities;

public class ServiceCaseTaskLink : BaseEntity
{
    public Guid ServiceCaseId { get; set; }
    public Guid TaskId { get; set; }
    public string Relationship { get; set; } = "FollowUp";

    public ServiceCase ServiceCase { get; set; } = default!;
    public TaskItem Task { get; set; } = default!;
}
