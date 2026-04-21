namespace Nebula.Domain.Entities;

public class AccountContact : BaseEntity
{
    public Guid AccountId { get; set; }
    public string FullName { get; set; } = default!;
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsPrimary { get; set; }

    public Account Account { get; set; } = default!;
}
