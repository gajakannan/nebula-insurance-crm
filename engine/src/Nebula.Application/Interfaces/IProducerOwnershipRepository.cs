using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface IProducerOwnershipRepository
{
    /// <summary>The current open (EffectiveTo == null) ownership period for a scope, tracked for update.</summary>
    Task<ProducerOwnership?> GetOpenPeriodAsync(string scopeType, Guid scopeId, CancellationToken ct = default);

    /// <summary>The ownership period covering <paramref name="asOf"/> for a scope, with ProducerNode included.</summary>
    Task<ProducerOwnership?> GetAsOfAsync(string scopeType, Guid scopeId, DateOnly asOf, CancellationToken ct = default);

    Task AddAsync(ProducerOwnership ownership, CancellationToken ct = default);
}
