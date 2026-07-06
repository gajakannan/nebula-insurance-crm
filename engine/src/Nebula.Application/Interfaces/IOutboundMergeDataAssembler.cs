using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IOutboundMergeDataAssembler
{
    Task<OutboundMergeContext> AssembleAsync(
        GeneratedDocumentRequestDto request,
        CancellationToken ct = default);
}
