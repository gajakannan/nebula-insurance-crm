using Nebula.Application.Common;
using Nebula.Application.DTOs;

namespace Nebula.Application.Services;

/// <summary>
/// Computes the source-visibility spec for search/report projections from the caller's
/// authorization context. Broad internal roles see all rows; region/owner-scoped roles
/// see only owner-matched or in-region projection rows. Applied at the query layer so
/// hidden-record existence never leaks via rows, counts, facets, or drilldowns.
/// </summary>
public static class ProjectionVisibilityResolver
{
    private static readonly string[] SeeAllRoles = ["Admin", "ProgramManager", "DistributionManager"];

    public static ProjectionVisibility For(ICurrentUserService user)
    {
        var seeAll = user.Roles.Any(r => SeeAllRoles.Contains(r));
        return new ProjectionVisibility(seeAll, user.Regions, user.UserId);
    }
}
