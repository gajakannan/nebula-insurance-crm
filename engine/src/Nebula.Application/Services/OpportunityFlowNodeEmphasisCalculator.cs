using Nebula.Application.DTOs;

namespace Nebula.Application.Services;

public static class OpportunityFlowNodeEmphasisCalculator
{
    public static IReadOnlyDictionary<string, string> Compute(
        IReadOnlyList<OpportunityFlowNodeDto> nodes)
    {
        var nonTerminalNodes = nodes
            .Where(node => !node.IsTerminal)
            .ToList();

        if (nonTerminalNodes.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var emphasisByStatus = nonTerminalNodes.ToDictionary(
            node => node.Status,
            _ => "normal",
            StringComparer.OrdinalIgnoreCase);

        var bottleneckNode = nonTerminalNodes
            .Where(node => node.CurrentCount > 0)
            .OrderByDescending(node => node.CurrentCount)
            .ThenByDescending(node => node.AvgDwellDays ?? double.MinValue)
            .ThenByDescending(node => node.DisplayOrder)
            .FirstOrDefault();

        var blockedNode = nonTerminalNodes
            .Where(node => !string.Equals(node.Status, bottleneckNode?.Status, StringComparison.OrdinalIgnoreCase))
            .Where(node => node.AvgDwellDays.HasValue)
            .OrderByDescending(node => node.AvgDwellDays)
            .ThenByDescending(node => node.CurrentCount)
            .ThenByDescending(node => node.DisplayOrder)
            .FirstOrDefault();

        var activeNode = nonTerminalNodes
            .Where(node => node.CurrentCount > 0)
            .Where(node => !string.Equals(node.Status, bottleneckNode?.Status, StringComparison.OrdinalIgnoreCase))
            .Where(node => !string.Equals(node.Status, blockedNode?.Status, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(node => node.DisplayOrder)
            .ThenByDescending(node => node.CurrentCount)
            .FirstOrDefault();

        if (activeNode is not null)
            emphasisByStatus[activeNode.Status] = "active";
        if (blockedNode is not null)
            emphasisByStatus[blockedNode.Status] = "blocked";
        if (bottleneckNode is not null)
            emphasisByStatus[bottleneckNode.Status] = "bottleneck";

        return emphasisByStatus;
    }
}
