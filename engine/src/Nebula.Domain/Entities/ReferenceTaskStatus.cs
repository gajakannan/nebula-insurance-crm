namespace Nebula.Domain.Entities;

public class ReferenceTaskStatus
{
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public short DisplayOrder { get; set; }
}
