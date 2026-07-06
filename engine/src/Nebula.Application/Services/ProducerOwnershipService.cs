using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

/// <summary>
/// F0017-S0003 (ADR-026): effective-dated producer ownership. Assignment closes the current open
/// period at <c>EffectiveFrom</c> and opens the new one in one transaction; rows are never overwritten.
/// At most one open period per (ScopeType, ScopeId) is enforced by a filtered unique index. "As of D"
/// reads return the period covering D.
/// </summary>
public class ProducerOwnershipService(
    IProducerOwnershipRepository ownershipRepo,
    IDistributionNodeRepository nodeRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    ILogger<ProducerOwnershipService> logger)
{
    private readonly ILogger<ProducerOwnershipService> _logger = logger;

    /// <summary>
    /// Assign or reassign ownership. Error codes: <c>producer_not_found</c> (404),
    /// <c>invalid_period</c> (422 — backdating to ≤ the current open period start). A concurrent
    /// double-open surfaces as a unique-violation <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
    /// (→ 409 overlap); a stale <paramref name="ifMatch"/> surfaces as DbUpdateConcurrencyException (→ 412).
    /// </summary>
    public async Task<(ProducerOwnershipDto? Dto, string? ErrorCode)> AssignAsync(
        ProducerOwnershipAssignmentRequestDto req, uint? ifMatch, ICurrentUserService user, CancellationToken ct = default)
    {
        var producer = await nodeRepo.GetByIdAsync(req.ProducerNodeId, ct);
        if (producer is null || !producer.IsActive)
            return (null, "producer_not_found");

        var now = DateTime.UtcNow;
        var open = await ownershipRepo.GetOpenPeriodAsync(req.ScopeType, req.ScopeId, ct);

        if (open is not null)
        {
            // A normal assign moves the timeline forward; backdating to/under the current period is a
            // correction-path operation, not a plain reassign.
            if (req.EffectiveFrom <= open.EffectiveFrom)
                return (null, "invalid_period");

            if (ifMatch.HasValue) open.RowVersion = ifMatch.Value; // optimistic concurrency on the open period
            open.EffectiveTo = req.EffectiveFrom;
            open.UpdatedAt = now;
            open.UpdatedByUserId = user.UserId;
        }

        var period = new ProducerOwnership
        {
            ScopeType = req.ScopeType,
            ScopeId = req.ScopeId,
            ProducerNodeId = req.ProducerNodeId,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = null,
            AssignmentReason = req.AssignmentReason,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };
        await ownershipRepo.AddAsync(period, ct);

        var reassigned = open is not null;
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "ProducerOwnership",
            EntityId = period.Id,
            EventType = reassigned ? "ProducerOwnershipReassigned" : "ProducerOwnershipAssigned",
            EventDescription = reassigned
                ? $"Producer ownership reassigned for {req.ScopeType} {req.ScopeId}"
                : $"Producer ownership assigned for {req.ScopeType} {req.ScopeId}",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                id = period.Id,
                scopeType = req.ScopeType,
                scopeId = req.ScopeId,
                producerNodeId = req.ProducerNodeId,
                previousProducerNodeId = open?.ProducerNodeId,
                effectiveFrom = req.EffectiveFrom.ToString("O"),
            }),
        }, ct);

        if (req.ScopeType == "BrokerRelationship")
        {
            await timelineRepo.AddEventAsync(new ActivityTimelineEvent
            {
                EntityType = "Broker",
                EntityId = req.ScopeId,
                EventType = reassigned ? "ProducerOwnershipReassigned" : "ProducerOwnershipAssigned",
                EventDescription = reassigned
                    ? $"Producer ownership reassigned from {open!.ProducerNodeId} to {req.ProducerNodeId} effective {req.EffectiveFrom:yyyy-MM-dd}"
                    : $"Producer ownership assigned to {req.ProducerNodeId} effective {req.EffectiveFrom:yyyy-MM-dd}",
                ActorUserId = user.UserId,
                ActorDisplayName = user.DisplayName,
                OccurredAt = now,
                EventPayloadJson = JsonSerializer.Serialize(new
                {
                    producerOwnershipId = period.Id,
                    scopeType = req.ScopeType,
                    scopeId = req.ScopeId,
                    oldProducerNodeId = open?.ProducerNodeId,
                    newProducerNodeId = req.ProducerNodeId,
                    effectiveFrom = req.EffectiveFrom.ToString("O"),
                    assignmentReason = req.AssignmentReason,
                }),
            }, ct);
        }

        await unitOfWork.CommitAsync(ct);

        _logger.LogInformation(
            "Producer ownership {Action} for {ScopeType} {ScopeId} -> producer {ProducerNodeId} effective {EffectiveFrom}",
            reassigned ? "reassigned" : "assigned", req.ScopeType, req.ScopeId, req.ProducerNodeId, req.EffectiveFrom);

        return (MapToDto(period, producer.DisplayName), null);
    }

    public async Task<ProducerOwnershipLookupResponseDto> GetAsOfAsync(
        string scopeType, Guid scopeId, DateOnly? asOf, CancellationToken ct = default)
    {
        var effectiveAsOf = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var period = await ownershipRepo.GetAsOfAsync(scopeType, scopeId, effectiveAsOf, ct);
        var dto = period is null ? null : MapToDto(period, period.ProducerNode?.DisplayName);
        return new ProducerOwnershipLookupResponseDto(scopeType, scopeId, effectiveAsOf, dto);
    }

    private static ProducerOwnershipDto MapToDto(ProducerOwnership p, string? producerName) => new(
        p.Id, p.ScopeType, p.ScopeId, p.ProducerNodeId, producerName,
        p.EffectiveFrom, p.EffectiveTo, p.AssignmentReason, p.RowVersion.ToString(),
        p.CreatedByUserId, p.CreatedAt);
}
