using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

/// <summary>
/// F0017-S0004 (ADR-026): territory definition + effective-dated membership. Active territory names
/// are unique (409 on duplicate). Member assignment closes the member's current open period at
/// EffectiveFrom and opens a new one atomically; a conflicting active overlap is rejected (409 via the
/// filtered unique index); backdating to ≤ the open period start is rejected (422). "As of D" reads
/// return the period covering D.
/// </summary>
public class TerritoryService(
    ITerritoryRepository territoryRepo,
    ITerritoryAssignmentRepository assignmentRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    ILogger<TerritoryService> logger)
{
    private readonly ILogger<TerritoryService> _logger = logger;

    public async Task<(TerritoryDto? Dto, string? ErrorCode)> CreateTerritoryAsync(
        TerritoryCreateRequestDto req, ICurrentUserService user, CancellationToken ct = default)
    {
        if (await territoryRepo.ExistsActiveByNameAsync(req.Name, ct))
            return (null, "duplicate_name");

        var now = DateTime.UtcNow;
        var territory = new Territory
        {
            Name = req.Name,
            Description = req.Description,
            CriteriaJson = JsonSerializer.Serialize(req.Criteria ?? new Dictionary<string, string>()),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };
        await territoryRepo.AddAsync(territory, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "Territory",
            EntityId = territory.Id,
            EventType = "TerritoryCreated",
            EventDescription = $"Territory \"{territory.Name}\" created",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new { id = territory.Id, name = territory.Name }),
        }, ct);

        await unitOfWork.CommitAsync(ct);
        return (MapTerritory(territory), null);
    }

    /// <summary>
    /// Error codes: <c>not_found</c> (404 — territory), <c>invalid_period</c> (422 — backdate ≤ open start).
    /// A concurrent double-open surfaces as a unique-violation DbUpdateException (→ 409 overlap); a stale
    /// <paramref name="ifMatch"/> on the territory surfaces as DbUpdateConcurrencyException (→ 412).
    /// </summary>
    public async Task<(TerritoryAssignmentDto? Dto, string? ErrorCode)> AssignMemberAsync(
        Guid territoryId, TerritoryMemberAssignmentRequestDto req, uint? ifMatch, ICurrentUserService user, CancellationToken ct = default)
    {
        var territory = await territoryRepo.GetByIdAsync(territoryId, ct);
        if (territory is null) return (null, "not_found");

        var now = DateTime.UtcNow;
        var open = await assignmentRepo.GetOpenForMemberAsync(req.MemberType, req.MemberId, ct);
        if (open is not null && req.EffectiveFrom <= open.EffectiveFrom)
            return (null, "invalid_period");

        if (open is not null)
        {
            open.EffectiveTo = req.EffectiveFrom;
            open.UpdatedAt = now;
            open.UpdatedByUserId = user.UserId;
        }

        // Member assignment touches the territory; tie optimistic concurrency to the territory rowVersion.
        if (ifMatch.HasValue) territory.RowVersion = ifMatch.Value;
        territory.UpdatedAt = now;
        territory.UpdatedByUserId = user.UserId;

        var assignment = new TerritoryAssignment
        {
            TerritoryId = territoryId,
            MemberType = req.MemberType,
            MemberId = req.MemberId,
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = null,
            AssignmentReason = req.AssignmentReason,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };
        await assignmentRepo.AddAsync(assignment, ct);

        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "TerritoryAssignment",
            EntityId = assignment.Id,
            EventType = open is null ? "TerritoryMemberAssigned" : "TerritoryMemberReassigned",
            EventDescription = $"{req.MemberType} {req.MemberId} assigned to territory \"{territory.Name}\"",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                id = assignment.Id,
                territoryId,
                memberType = req.MemberType,
                memberId = req.MemberId,
                previousTerritoryId = open?.TerritoryId,
                effectiveFrom = req.EffectiveFrom.ToString("O"),
            }),
        }, ct);

        if (req.MemberType == "Broker")
        {
            await timelineRepo.AddEventAsync(new ActivityTimelineEvent
            {
                EntityType = "Broker",
                EntityId = req.MemberId,
                EventType = open is null ? "TerritoryMemberAssigned" : "TerritoryMemberReassigned",
                EventDescription = open is null
                    ? $"Territory assigned to \"{territory.Name}\" effective {req.EffectiveFrom:yyyy-MM-dd}"
                    : $"Territory reassigned from {open.TerritoryId} to \"{territory.Name}\" effective {req.EffectiveFrom:yyyy-MM-dd}",
                ActorUserId = user.UserId,
                ActorDisplayName = user.DisplayName,
                OccurredAt = now,
                EventPayloadJson = JsonSerializer.Serialize(new
                {
                    territoryAssignmentId = assignment.Id,
                    memberType = req.MemberType,
                    memberId = req.MemberId,
                    oldTerritoryId = open?.TerritoryId,
                    newTerritoryId = territoryId,
                    effectiveFrom = req.EffectiveFrom.ToString("O"),
                    assignmentReason = req.AssignmentReason,
                }),
            }, ct);
        }

        await unitOfWork.CommitAsync(ct);

        _logger.LogInformation(
            "Territory member {Action}: {MemberType} {MemberId} -> territory {TerritoryId} effective {EffectiveFrom}",
            open is null ? "assigned" : "reassigned", req.MemberType, req.MemberId, territoryId, req.EffectiveFrom);

        return (MapAssignment(assignment, territory.Name, null), null);
    }

    public async Task<(PaginatedResult<TerritoryAssignmentDto>? Result, string? ErrorCode)> ListMembersAsync(
        Guid territoryId, DateOnly? asOf, int page, int pageSize, CancellationToken ct = default)
    {
        var territory = await territoryRepo.GetByIdAsync(territoryId, ct);
        if (territory is null) return (null, "not_found");

        var effectiveAsOf = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var boundedPage = Math.Max(page, 1);
        var boundedPageSize = Math.Clamp(pageSize, 1, 100);
        var (data, total) = await assignmentRepo.ListMembersAsOfAsync(territoryId, effectiveAsOf, boundedPage, boundedPageSize, ct);

        var mapped = data.Select(a => MapAssignment(a, territory.Name, null)).ToList();
        return (new PaginatedResult<TerritoryAssignmentDto>(mapped, boundedPage, boundedPageSize, total), null);
    }

    public async Task<TerritoryAssignmentLookupResponseDto> GetForMemberAsync(
        string memberType, Guid memberId, DateOnly? asOf, CancellationToken ct = default)
    {
        var effectiveAsOf = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var assignment = await assignmentRepo.GetForMemberAsOfAsync(memberType, memberId, effectiveAsOf, ct);
        var dto = assignment is null ? null : MapAssignment(assignment, assignment.Territory?.Name, null);
        return new TerritoryAssignmentLookupResponseDto(memberType, memberId, effectiveAsOf, dto);
    }

    private static TerritoryDto MapTerritory(Territory t) => new(
        t.Id, t.Name, t.Description,
        JsonSerializer.Deserialize<Dictionary<string, string>>(t.CriteriaJson) ?? new Dictionary<string, string>(),
        t.IsActive, t.RowVersion.ToString(), t.CreatedByUserId, t.CreatedAt);

    private static TerritoryAssignmentDto MapAssignment(TerritoryAssignment a, string? territoryName, string? memberDisplayName) => new(
        a.Id, a.TerritoryId, territoryName, a.MemberType, a.MemberId, memberDisplayName,
        a.EffectiveFrom, a.EffectiveTo, a.AssignmentReason, a.RowVersion.ToString(), a.CreatedByUserId, a.CreatedAt);
}
