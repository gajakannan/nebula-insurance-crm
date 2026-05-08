namespace Nebula.Application.Services;

public static class LobSchemaDefaults
{
    public const string EmptyAttributesJson = "{}";
    public static readonly Guid UnspecifiedProductVersionId = Guid.Parse("aa901058-2402-5370-9978-66eb184066be");
    public static readonly Guid CyberProductVersionId = Guid.Parse("48f5f86a-7396-50bf-92dd-a3a36fe63c20");

    public static readonly IReadOnlyDictionary<string, Guid> LegacyProductVersionIds =
        new Dictionary<string, Guid>(StringComparer.Ordinal)
        {
            ["Property"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000001"),
            ["GeneralLiability"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000002"),
            ["CommercialAuto"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000003"),
            ["WorkersCompensation"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000004"),
            ["ProfessionalLiability"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000005"),
            ["Marine"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000006"),
            ["Umbrella"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000007"),
            ["Surety"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000008"),
            ["Cyber"] = Guid.Parse("4ffc79e6-4e32-5d39-a82c-891b6034ab9e"),
            ["DirectorsOfficers"] = Guid.Parse("7b8f0034-0001-5000-9000-000000000010"),
        };

    public static Guid ResolveDefaultProductVersionId(string? lineOfBusiness)
    {
        if (string.IsNullOrWhiteSpace(lineOfBusiness))
            return UnspecifiedProductVersionId;

        return LegacyProductVersionIds.TryGetValue(lineOfBusiness, out var legacyVersionId)
            ? legacyVersionId
            : UnspecifiedProductVersionId;
    }
}
