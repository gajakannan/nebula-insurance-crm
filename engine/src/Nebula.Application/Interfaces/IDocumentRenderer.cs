using Nebula.Application.DTOs;

namespace Nebula.Application.Interfaces;

public interface IDocumentRenderer
{
    Task<RenderedDocumentBinary> RenderAsync(
        DocumentSidecarDto template,
        OutboundMergeContext context,
        CancellationToken ct = default);
}
