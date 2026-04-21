namespace Nebula.Domain.Workflow;

public sealed record LineOfBusinessDefinition(string Code, string Label);

public static class LineOfBusinessCatalog
{
    public static readonly IReadOnlyList<LineOfBusinessDefinition> Definitions =
    [
        new("Property", "Property"),
        new("GeneralLiability", "General Liability"),
        new("CommercialAuto", "Commercial Auto"),
        new("WorkersCompensation", "Workers' Compensation"),
        new("ProfessionalLiability", "Professional Liability / E&O"),
        new("Marine", "Marine / Inland Marine"),
        new("Umbrella", "Umbrella / Excess"),
        new("Surety", "Surety / Bond"),
        new("Cyber", "Cyber Liability"),
        new("DirectorsOfficers", "Directors & Officers"),
    ];

    public static readonly IReadOnlySet<string> KnownCodes = Definitions
        .Select(definition => definition.Code)
        .ToHashSet(StringComparer.Ordinal);

    public static bool IsValid(string? code) =>
        code is null || KnownCodes.Contains(code);

    public static string GetDisplayLabel(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "Unknown";

        return Definitions.FirstOrDefault(definition => definition.Code == code)?.Label ?? code;
    }
}
