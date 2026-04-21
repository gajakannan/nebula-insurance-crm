using Nebula.Application.Common;
using Nebula.Application.Interfaces;

namespace Nebula.Application.Services;

/// <summary>
/// Centralizes broker scope resolution for BrokerUser requests (F0009 §6 / §6.1).
///
/// Resolution contract:
///   1. Read broker_tenant_id claim from ICurrentUserService.BrokerTenantId.
///   2. Query IBrokerRepository for exactly one active Broker matching that tenant ID.
///   3. Zero or multiple matches → throw BrokerScopeUnresolvableException.
///
/// Callers MUST invoke ResolveAsync() before any broker-scoped data access when the
/// authenticated user has the BrokerUser role. The global exception handler maps
/// BrokerScopeUnresolvableException to HTTP 403 with code "broker_scope_unresolvable".
/// </summary>
public class BrokerScopeResolver(IBrokerRepository brokerRepo)
{
    /// <summary>
    /// Resolves the authenticated BrokerUser's broker scope.
    /// </summary>
    /// <param name="user">The current user service providing the broker_tenant_id claim.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resolved Broker.Id.</returns>
    /// <exception cref="BrokerScopeUnresolvableException">
    /// Thrown when broker_tenant_id is missing, or when zero or more than one active
    /// broker matches the claim.
    /// </exception>
    public async Task<Guid> ResolveAsync(ICurrentUserService user, CancellationToken ct = default)
    {
        var tenantId = user.BrokerTenantId;
        if (string.IsNullOrEmpty(tenantId))
            throw new BrokerScopeUnresolvableException();

        var brokerId = await brokerRepo.GetIdByBrokerTenantIdAsync(tenantId, ct);
        if (brokerId is null)
            throw new BrokerScopeUnresolvableException();

        return brokerId.Value;
    }
}
