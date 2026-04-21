namespace Nebula.Domain.Entities;

public class ReferenceSubmissionStatus
{
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool IsTerminal { get; set; }
    public short DisplayOrder { get; set; }
    public string? ColorGroup { get; set; }
}
