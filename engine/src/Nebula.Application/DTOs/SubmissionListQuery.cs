namespace Nebula.Application.DTOs;

public record SubmissionListQuery(
    string? Status = null,
    Guid? BrokerId = null,
    Guid? AccountId = null,
    string? LineOfBusiness = null,
    Guid? AssignedToUserId = null,
    bool? Stale = null,
    bool? ApprovalPending = null,
    bool? StuckOnly = null,
    bool IncludeArchived = false,
    string Sort = "createdAt",
    string SortDir = "desc",
    int Page = 1,
    int PageSize = 25);
