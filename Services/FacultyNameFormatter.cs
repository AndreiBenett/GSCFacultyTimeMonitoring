namespace GSCFacultyTimeMonitoring.Services;

/// <summary>
/// Presents names as "Surname, Given names" without rewriting existing database records.
/// For ambiguous/multipart surnames, admins should store "Surname, Given names" explicitly.
/// </summary>
public static class FacultyNameFormatter
{
    private static readonly HashSet<string> SurnamePrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "de", "del", "dela", "da", "dos", "das", "di", "la", "las", "los", "san", "santa", "van", "von"
    };

    public static string SurnameFirst(string? fullName)
    {
        var name = (fullName ?? string.Empty).Trim();
        if (name.Length == 0) return string.Empty;
        if (name.Contains(',')) return name; // Explicit surname-first format has priority.

        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2) return name;

        var surnameStart = words.Length - 1;
        // "Juan Dela Cruz", "Ana De La Cruz", "Jose San Pedro".
        while (surnameStart > 1 && SurnamePrefixes.Contains(words[surnameStart - 1]))
            surnameStart--;

        return $"{string.Join(" ", words[surnameStart..])}, {string.Join(" ", words[..surnameStart])}";
    }

    public static string Initial(string? fullName)
    {
        var surnameFirst = SurnameFirst(fullName);
        return surnameFirst.Length > 0 ? char.ToUpperInvariant(surnameFirst[0]).ToString() : "#";
    }
}
