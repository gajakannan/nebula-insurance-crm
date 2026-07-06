using System.Text;
using System.Text.Json;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Documents;

public sealed class SimplePdfDocumentRenderer : IDocumentRenderer
{
    public Task<RenderedDocumentBinary> RenderAsync(
        DocumentSidecarDto template,
        OutboundMergeContext context,
        CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            title = $"{context.ArtifactFamily.ToUpperInvariant()} generated document",
            templateId = template.DocumentId,
            templateName = template.LogicalName,
            sourceSnapshotHash = context.SourceSnapshotHash,
            values = context.Values,
        });

        var escaped = EscapePdfText(payload);
        var body = $"BT /F1 10 Tf 36 760 Td ({escaped}) Tj ET";
        var pdf = $"""
%PDF-1.4
1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj
2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj
3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj
4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj
5 0 obj << /Length {Encoding.ASCII.GetByteCount(body)} >> stream
{body}
endstream endobj
xref
0 6
0000000000 65535 f 
trailer << /Root 1 0 R /Size 6 >>
startxref
0
%%EOF
""";
        var bytes = Encoding.ASCII.GetBytes(pdf);
        return Task.FromResult(new RenderedDocumentBinary(
            new MemoryStream(bytes),
            "application/pdf",
            $"{context.ArtifactFamily}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf",
            bytes.LongLength));
    }

    private static string EscapePdfText(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
