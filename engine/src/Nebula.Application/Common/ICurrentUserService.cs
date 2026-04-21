namespace Nebula.Application.Common;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string? DisplayName { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyList<string> Regions { get; }
    string? BrokerTenantId { get; }
}
