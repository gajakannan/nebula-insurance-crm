using Nebula.Application.Common;
using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IBrokerRepository
{
    Task<Broker?> GetByIdAsync(Guid id, CancellationToken ct = default);
    /// <summary>Bypasses the soft-delete global query filter. Use only for Admin Broker 360 and reactivation (F0002-S0008).</summary>
    Task<Broker?> GetByIdIncludingDeactivatedAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResult<Broker>> ListAsync(string? search, string? statusFilter, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Broker broker, CancellationToken ct = default);
    Task UpdateAsync(Broker broker, CancellationToken ct = default);
    Task<bool> ExistsByLicenseAsync(string licenseNumber, CancellationToken ct = default);
    Task<bool> HasActiveSubmissionsOrRenewalsAsync(Guid brokerId, CancellationToken ct = default);
    /// <summary>
    /// Resolves a broker_tenant_id claim to a single active Broker.Id for BrokerUser scope enforcement (F0009 §6).
    /// Returns null if zero or multiple active brokers match (caller must treat as scope unresolvable).
    /// </summary>
    Task<Guid?> GetIdByBrokerTenantIdAsync(string brokerTenantId, CancellationToken ct = default);
}
