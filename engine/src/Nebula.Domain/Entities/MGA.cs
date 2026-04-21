namespace Nebula.Domain.Entities;

public class MGA : BaseEntity
{
    public string Name { get; set; } = default!;
    public string ExternalCode { get; set; } = default!;
    public string Status { get; set; } = default!;
}
