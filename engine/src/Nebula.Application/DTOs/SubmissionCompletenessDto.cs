namespace Nebula.Application.DTOs;

public record SubmissionCompletenessDto(
    bool IsComplete,
    IReadOnlyList<SubmissionFieldCheckDto> FieldChecks,
    IReadOnlyList<SubmissionDocumentCheckDto> DocumentChecks,
    IReadOnlyList<string> MissingItems);
