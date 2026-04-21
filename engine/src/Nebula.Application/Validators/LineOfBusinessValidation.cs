using Nebula.Domain.Workflow;

namespace Nebula.Application.Validators;

internal static class LineOfBusinessValidation
{
    private static readonly string AllowedValues = string.Join(", ", LineOfBusinessCatalog.KnownCodes.OrderBy(code => code));

    public static bool IsValid(string? lineOfBusiness) => LineOfBusinessCatalog.IsValid(lineOfBusiness);

    public static string ErrorMessage => $"LineOfBusiness must be one of: {AllowedValues}.";
}
