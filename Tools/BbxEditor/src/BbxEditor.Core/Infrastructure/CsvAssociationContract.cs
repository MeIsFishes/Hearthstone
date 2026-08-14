namespace BbxEditor.Infrastructure;

public static class CsvAssociationContract
{
    public const string Prefix = "// Associated: ";

    public static bool TryParse(
        string? comment,
        out IReadOnlyList<string> tableNames,
        out string error)
    {
        if (comment is null || !comment.StartsWith(Prefix, StringComparison.Ordinal))
        {
            tableNames = [];
            error = "The association comment must start with '// Associated: '.";
            return false;
        }

        var value = comment[Prefix.Length..];
        if (value == "None")
        {
            tableNames = [];
            error = string.Empty;
            return true;
        }

        var names = value.Split(", ", StringSplitOptions.None);
        if (names.Length == 0 || names.Any(name => name.Length == 0 ||
                !char.IsLetter(name[0]) ||
                name.Any(character => !char.IsLetterOrDigit(character) && character != '_')))
        {
            tableNames = [];
            error = "Associated table names must be non-empty identifiers separated by ', '.";
            return false;
        }
        if (names.Distinct(StringComparer.Ordinal).Count() != names.Length)
        {
            tableNames = [];
            error = "Associated table names must not be repeated.";
            return false;
        }
        if (!names.SequenceEqual(names.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal))
        {
            tableNames = [];
            error = "Associated table names must be sorted using ordinal order.";
            return false;
        }

        tableNames = names;
        error = string.Empty;
        return true;
    }
}
