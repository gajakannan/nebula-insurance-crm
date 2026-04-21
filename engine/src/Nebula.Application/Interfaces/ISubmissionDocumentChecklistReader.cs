using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface ISubmissionDocumentChecklistReader
{
    Task<IReadOnlyList<SubmissionDocumentCheckDto>> GetChecklistAsync(
        Guid submissionId,
        CancellationToken ct = default);
}
