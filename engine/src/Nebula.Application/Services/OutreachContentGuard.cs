using System.Text.RegularExpressions;

namespace Nebula.Application.Services;

// F0038-S0005/S0006 — engine-side content constraint for outreach drafts (intake: a draft must
// NOT state or imply premium, quote figures, coverage terms, or a binding commitment). This is
// defense in depth behind the Neuron drafter's own guard; the engine persists nothing that fails.
public static partial class OutreachContentGuard
{
    private static readonly (Regex Pattern, string Category)[] Forbidden =
    [
        (PremiumRegex(), "premium"),
        (QuoteRegex(), "quote"),
        (BindingRegex(), "binding-commitment"),
        (TermsRegex(), "coverage-terms"),
        (CurrencyRegex(), "monetary-amount"),
    ];

    /// <summary>Returns a short reason string when the body violates the constraint, else null.</summary>
    public static string? Validate(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "empty draft body";

        foreach (var (pattern, category) in Forbidden)
        {
            if (pattern.IsMatch(body))
                return $"draft must not include {category} language";
        }

        return null;
    }

    [GeneratedRegex(@"\bpremiums?\b", RegexOptions.IgnoreCase)]
    private static partial Regex PremiumRegex();

    [GeneratedRegex(@"\bquot(e|es|ed|ing)\b", RegexOptions.IgnoreCase)]
    private static partial Regex QuoteRegex();

    [GeneratedRegex(@"\b(bind|binding|bound)\b", RegexOptions.IgnoreCase)]
    private static partial Regex BindingRegex();

    [GeneratedRegex(@"\b(deductible|coverage limit|policy limit|terms and conditions)\b", RegexOptions.IgnoreCase)]
    private static partial Regex TermsRegex();

    [GeneratedRegex(@"\$\s?\d", RegexOptions.IgnoreCase)]
    private static partial Regex CurrencyRegex();
}
