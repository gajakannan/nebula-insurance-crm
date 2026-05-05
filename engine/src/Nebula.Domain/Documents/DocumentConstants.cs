namespace Nebula.Domain.Documents;

public static class DocumentConstants
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;
    public const long MaxBatchSizeBytes = 50 * 1024 * 1024;
    public const int MaxBatchFiles = 25;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public const string SystemQuarantineWorker = "system:quarantine-worker";
    public const string SystemRetentionSweeper = "system:retention-sweeper";

    public static readonly IReadOnlySet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".docx",
        ".xlsx",
        ".csv",
    };

    public static readonly IReadOnlyDictionary<string, string[]> AllowedContentTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".png"] = ["image/png"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            [".csv"] = ["text/csv", "application/csv", "application/vnd.ms-excel"],
        };

    public static readonly IReadOnlySet<string> Classifications = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "public",
        "confidential",
        "restricted",
    };

    public static readonly IReadOnlySet<string> Statuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "quarantined",
        "available",
        "failed_promote",
    };

    public static readonly IReadOnlySet<string> ParentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "account",
        "submission",
        "policy",
        "renewal",
    };

    public static bool IsRenderablePreviewExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }
}
