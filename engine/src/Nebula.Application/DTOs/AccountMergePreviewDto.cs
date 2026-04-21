namespace Nebula.Application.DTOs;

public record AccountMergePreviewDto(
    Guid SourceAccountId,
    Guid SurvivorAccountId,
    string SourceDisplayName,
    string SurvivorDisplayName,
    int SubmissionCount,
    int PolicyCount,
    int RenewalCount,
    int ContactCount,
    int TimelineCount,
    int TotalLinked,
    int Threshold);
