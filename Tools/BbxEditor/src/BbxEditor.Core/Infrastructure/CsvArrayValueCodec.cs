namespace BbxEditor.Infrastructure;

public static class CsvArrayValueCodec
{
    public const char Separator = ';';

    public static IReadOnlyList<string> Decode(string? value) =>
        string.IsNullOrEmpty(value) ? [] : value.Split(Separator, StringSplitOptions.None);

    public static string Encode(IEnumerable<string> values) =>
        string.Join(Separator, values.Select(value => value ?? string.Empty));
}
