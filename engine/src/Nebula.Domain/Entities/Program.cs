namespace Nebula.Domain.Entities;

public class Program : BaseEntity
{
    public string Name { get; set; } = default!;
    public string ProgramCode { get; set; } = default!;
    public Guid MgaId { get; set; }
    public Guid? ManagedByUserId { get; set; }

    public MGA Mga { get; set; } = default!;
}
