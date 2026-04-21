namespace Nebula.Domain.Entities;

public class Contact : BaseEntity
{
    public Guid? BrokerId { get; set; }
    public Guid? AccountId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Phone { get; set; } = default!;
    public string Role { get; set; } = default!;

    public Broker? Broker { get; set; }
    public Account? Account { get; set; }
}
