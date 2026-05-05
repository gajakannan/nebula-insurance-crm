using Nebula.Application.Interfaces;

namespace Nebula.Infrastructure.Documents;

public sealed class MockTimerScanner : IQuarantineScanner
{
    public Task<ScanResult> ScanAsync(QuarantineEntryDto entry, CancellationToken ct = default) =>
        Task.FromResult<ScanResult>(new ScanResult.Clean());
}
