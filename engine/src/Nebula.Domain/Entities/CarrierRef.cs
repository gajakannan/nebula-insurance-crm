namespace Nebula.Domain.Entities;

public class CarrierRef : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? NaicCode { get; set; }
    public bool IsActive { get; set; } = true;
}
