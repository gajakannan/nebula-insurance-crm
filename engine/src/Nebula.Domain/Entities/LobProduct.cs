namespace Nebula.Domain.Entities;

public class LobProduct : BaseEntity
{
    public string ProductKey { get; set; } = default!;
    public string? LineOfBusiness { get; set; }
    public string Name { get; set; } = default!;
    public string Status { get; set; } = "Active";
    public string? Description { get; set; }

    public ICollection<LobProductVersion> Versions { get; set; } = [];
}
