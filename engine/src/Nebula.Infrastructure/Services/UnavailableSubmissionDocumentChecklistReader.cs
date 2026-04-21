using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Services;

public sealed class UnavailableSubmissionDocumentChecklistReader : ISubmissionDocumentChecklistReader
{
    public Task<IReadOnlyList<SubmissionDocumentCheckDto>> GetChecklistAsync(
        Guid submissionId,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SubmissionDocumentCheckDto>>(
        [
            new("Application", true, "unavailable"),
            new("Supporting Document", true, "unavailable"),
        ]);
}
