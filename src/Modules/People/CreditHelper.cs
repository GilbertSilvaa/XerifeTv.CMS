using XerifeTv.CMS.Modules.Abstractions.Enums;
using XerifeTv.CMS.Modules.Abstractions.ValueObjects;

namespace XerifeTv.CMS.Modules.People;

public static class CreditHelper
{
    public static string Normalize(string? value)
        => (value ?? string.Empty).Trim().ToLowerInvariant();

    public static string BuildKey(Credit credit)
        => credit.Type == ECreditType.Cast
            ? $"{(int)credit.Type}|{Normalize(credit.PersonId)}|{Normalize(credit.Character)}"
            : $"{(int)credit.Type}|{Normalize(credit.PersonId)}|{Normalize(credit.Department)}|{Normalize(credit.Job)}";

    public static bool IsEquivalent(Credit first, Credit second)
        => string.Equals(BuildKey(first), BuildKey(second), StringComparison.Ordinal);

    public static List<Credit> Merge(
        IEnumerable<Credit> existing,
        IEnumerable<Credit> incoming,
        out int imported,
        out int ignored)
    {
        var result = existing.ToList();
        var keys = result.Select(BuildKey).ToHashSet(StringComparer.Ordinal);

        imported = 0;
        ignored = 0;

        foreach (var credit in incoming)
        {
            if (!keys.Add(BuildKey(credit)))
            {
                ignored++;
                continue;
            }

            result.Add(credit);
            imported++;
        }

        return result;
    }
}
