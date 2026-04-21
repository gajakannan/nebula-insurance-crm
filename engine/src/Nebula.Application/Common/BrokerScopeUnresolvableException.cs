namespace Nebula.Application.Common;

/// <summary>
/// Thrown when a BrokerUser's broker_tenant_id claim cannot be resolved to exactly one
/// active Broker entity (F0009 Implementation Contract §6 / §6.1).
///
/// The global exception handler maps this to:
///   HTTP 403 application/problem+json { code: "broker_scope_unresolvable" }
///
/// This is NOT a session teardown trigger — the JWT is valid and the OIDC session is intact.
/// </summary>
public sealed class BrokerScopeUnresolvableException : Exception
{
    public BrokerScopeUnresolvableException()
        : base("Broker scope could not be resolved.")
    {
    }
}
