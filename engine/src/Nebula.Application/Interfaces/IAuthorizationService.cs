namespace Nebula.Application.Interfaces;

public interface IAuthorizationService
{
    Task<bool> AuthorizeAsync(string userRole, string resourceType, string action, IDictionary<string, object>? resourceAttributes = null);
}
