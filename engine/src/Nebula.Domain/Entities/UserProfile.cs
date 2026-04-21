namespace Nebula.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string IdpIssuer { get; set; } = default!;
    public string IdpSubject { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Department { get; set; } = default!;
    public string RegionsJson { get; set; } = "[]";
    public string RolesJson { get; set; } = "[]";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
