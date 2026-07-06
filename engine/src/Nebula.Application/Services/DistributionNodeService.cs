using System.Text.Json;
using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

/// <summary>
/// F0017-S0001/S0002 (ADR-026): self-referencing distribution hierarchy with a materialized ancestry
/// cache. Reparent enforces no-self-parent / no-cycle / no-orphan and recomputes the node + all
/// descendants transactionally, emitting immutable timeline events. Reads use the cached path.
/// </summary>
public class DistributionNodeService(
    IDistributionNodeRepository nodeRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork,
    ILogger<DistributionNodeService> logger)
{
    private readonly ILogger<DistributionNodeService> _logger = logger;

    /// <summary>
    /// Set or clear a node's parent. Returns an error code on rule violation:
    /// <c>not_found</c>, <c>self_parent</c>, <c>invalid_parent</c>, <c>cycle</c>.
    /// Stale rowVersion surfaces as <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/> (→ 412).
    /// </summary>
    public async Task<(DistributionNodeDto? Dto, string? ErrorCode)> SetParentAsync(
        Guid nodeId, Guid? parentId, string? note, uint rowVersion, ICurrentUserService user, CancellationToken ct = default)
    {
        var node = await nodeRepo.GetByIdAsync(nodeId, ct);
        if (node is null) return (null, "not_found");

        if (parentId == nodeId) return (null, "self_parent");

        DistributionNode? parent = null;
        if (parentId.HasValue)
        {
            parent = await nodeRepo.GetByIdAsync(parentId.Value, ct);
            if (parent is null || !parent.IsActive) return (null, "invalid_parent");

            // Cycle guard: the proposed parent must not be the node itself or one of its descendants.
            var nodeSelfPrefix = SelfPrefix(node);
            if (parent.AncestryPath == nodeSelfPrefix || parent.AncestryPath.StartsWith(nodeSelfPrefix + "/"))
                return (null, "cycle");
        }

        var now = DateTime.UtcNow;
        var correlationId = Guid.NewGuid();

        // Capture pre-move state used to relocate the subtree.
        var oldSelfPrefix = SelfPrefix(node);
        var oldParentId = node.ParentId;
        var oldDepth = node.Depth;

        // New ancestry for the node (root→parent, excluding self).
        var newAncestry = parent is null ? "" : Combine(parent.AncestryPath, parent.Id);
        var newDepth = Segments(newAncestry).Count;
        var newSelfPrefix = Combine(newAncestry, node.Id);

        // Optimistic concurrency is anchored on the node's xmin.
        node.RowVersion = rowVersion;

        // Relocate descendants (replace the prefix up to and including this node) before mutating the node.
        var subtree = await nodeRepo.ListSubtreeAsync(oldSelfPrefix, ct);
        var descendantOldDepths = subtree.ToDictionary(d => d.Id, d => d.Depth);
        foreach (var d in subtree)
        {
            d.AncestryPath = newSelfPrefix + d.AncestryPath[oldSelfPrefix.Length..];
            d.Depth = Segments(d.AncestryPath).Count;
            d.UpdatedAt = now;
            d.UpdatedByUserId = user.UserId;
        }

        node.ParentId = parentId;
        node.AncestryPath = newAncestry;
        node.Depth = newDepth;
        node.UpdatedAt = now;
        node.UpdatedByUserId = user.UserId;

        // Maintain direct child counts on the affected parents.
        if (oldParentId != parentId)
        {
            if (oldParentId.HasValue)
            {
                var oldParent = await nodeRepo.GetByIdAsync(oldParentId.Value, ct);
                if (oldParent is not null && oldParent.ChildCount > 0) oldParent.ChildCount--;
            }
            if (parent is not null) parent.ChildCount++;
        }

        await EmitReparentEventAsync(node, oldParentId, parentId, oldDepth, newDepth, note, correlationId, user, now, ct);
        foreach (var d in subtree)
            await EmitReparentEventAsync(d, d.ParentId, d.ParentId, descendantOldDepths[d.Id], d.Depth, null, correlationId, user, now, ct);

        await unitOfWork.CommitAsync(ct);

        _logger.LogInformation(
            "DistributionNode {NodeId} reparented from {OldParent} to {NewParent} ({DescendantCount} descendants recomputed) correlationId={CorrelationId}",
            node.Id, oldParentId, parentId, subtree.Count, correlationId);

        return (MapToDto(node), null);
    }

    public async Task<DistributionNodeAncestorsResponseDto?> GetAncestorsAsync(Guid nodeId, CancellationToken ct = default)
    {
        var node = await nodeRepo.GetByIdAsync(nodeId, ct);
        if (node is null) return null;

        var ancestorIds = Segments(node.AncestryPath);
        var ancestors = await nodeRepo.GetByIdsAsync(ancestorIds, ct);
        // Preserve root→parent order from the materialized path.
        var byId = ancestors.ToDictionary(a => a.Id);
        var ordered = ancestorIds.Where(byId.ContainsKey).Select(id => MapToDto(byId[id])).ToList();

        return new DistributionNodeAncestorsResponseDto(MapToDto(node), ordered);
    }

    public async Task<(PaginatedResult<DistributionNodeDto>? Result, string? ErrorCode)> ListDescendantsAsync(
        Guid nodeId, int? depth, int page, int pageSize, CancellationToken ct = default)
    {
        var node = await nodeRepo.GetByIdAsync(nodeId, ct);
        if (node is null) return (null, "not_found");

        var boundedDepth = Math.Clamp(depth ?? 2, 1, 5);
        var boundedPageSize = Math.Clamp(pageSize, 1, 100);
        var selfPrefix = SelfPrefix(node);

        var (data, total) = await nodeRepo.ListDescendantPageAsync(
            selfPrefix, node.Depth, boundedDepth, Math.Max(page, 1), boundedPageSize, ct);

        var mapped = data.Select(MapToDto).ToList();
        return (new PaginatedResult<DistributionNodeDto>(mapped, Math.Max(page, 1), boundedPageSize, total), null);
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static string SelfPrefix(DistributionNode n) => Combine(n.AncestryPath, n.Id);

    private static string Combine(string ancestry, Guid id) =>
        (string.IsNullOrEmpty(ancestry) ? "" : ancestry) + "/" + id;

    private static List<Guid> Segments(string path) =>
        string.IsNullOrEmpty(path)
            ? []
            : path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList();

    private static DistributionNodeDto MapToDto(DistributionNode n) => new(
        n.Id, n.NodeType, n.DisplayName, n.ParentId,
        Segments(n.AncestryPath), n.Depth, n.ChildCount, n.IsActive, n.RowVersion.ToString());

    private async Task EmitReparentEventAsync(
        DistributionNode node, Guid? oldParentId, Guid? newParentId, int oldDepth, int newDepth,
        string? note, Guid correlationId, ICurrentUserService user, DateTime now, CancellationToken ct)
    {
        await timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = "DistributionNode",
            EntityId = node.Id,
            EventType = "DistributionNodeReparented",
            EventDescription = newParentId is null
                ? $"{node.DisplayName} moved to (root)"
                : $"{node.DisplayName} re-parented",
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = now,
            EventPayloadJson = JsonSerializer.Serialize(new
            {
                id = node.Id,
                nodeType = node.NodeType,
                oldParentId,
                newParentId,
                oldDepth,
                newDepth = node.Depth,
                note,
                correlationId,
            }),
        }, ct);

        if (node.NodeType == "Broker")
        {
            await timelineRepo.AddEventAsync(new ActivityTimelineEvent
            {
                EntityType = "Broker",
                EntityId = node.Id,
                EventType = "DistributionNodeReparented",
                EventDescription = newParentId is null
                    ? $"Distribution hierarchy changed: {node.DisplayName} moved from {oldParentId?.ToString() ?? "(root)"} to (root)"
                    : $"Distribution hierarchy changed: {node.DisplayName} moved from {oldParentId?.ToString() ?? "(root)"} to {newParentId}",
                ActorUserId = user.UserId,
                ActorDisplayName = user.DisplayName,
                OccurredAt = now,
                EventPayloadJson = JsonSerializer.Serialize(new
                {
                    distributionNodeId = node.Id,
                    nodeType = node.NodeType,
                    oldParentId,
                    newParentId,
                    oldDepth,
                    newDepth = node.Depth,
                    note,
                    correlationId,
                }),
            }, ct);
        }
    }
}
