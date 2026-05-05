namespace Nebula.Application.Interfaces;

public interface IQuarantineScanner
{
    Task<ScanResult> ScanAsync(QuarantineEntryDto entry, CancellationToken ct = default);
}

public abstract record ScanResult
{
    public sealed record Clean : ScanResult;
    public sealed record Infected(string Reason) : ScanResult;
    public sealed record Inconclusive(string Reason) : ScanResult;
}
