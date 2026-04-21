namespace Nebula.Application.DTOs;

public record AccountMergeImpactProjection(
    int SubmissionCount,
    int PolicyCount,
    int RenewalCount,
    int ContactCount,
    int TimelineCount)
{
    public int TotalLinked => SubmissionCount + PolicyCount + RenewalCount + ContactCount + TimelineCount;
}
