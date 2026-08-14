using System.Globalization;
using System.Text;

namespace BbxEditor.Infrastructure;

public enum TaskBlackboardInjectionValueType
{
    Bool,
    Int,
    Long,
    Float,
    Double,
    String,
}

public sealed record TaskBlackboardInjectionValue(
    string Key,
    TaskBlackboardInjectionValueType Type,
    string Value);

public static class TaskBlackboardInjectionCodec
{
    public static bool TryParse(
        string text,
        out IReadOnlyList<TaskBlackboardInjectionValue> values,
        out string error)
    {
        if (string.IsNullOrEmpty(text))
        {
            values = [];
            error = string.Empty;
            return true;
        }

        if (!TrySplitEscaped(text, ';', int.MaxValue, out var entries, out error))
        {
            values = [];
            return false;
        }

        var result = new List<TaskBlackboardInjectionValue>(entries.Count);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < entries.Count; index++)
        {
            if (entries[index].Length == 0 ||
                !TrySplitEscaped(entries[index], ',', 3, out var fields, out error) || fields.Count != 3 ||
                !TryUnescape(fields[0], out var key, out error) ||
                !TryUnescape(fields[1], out var typeText, out error) ||
                !TryUnescape(fields[2], out var value, out error))
            {
                values = [];
                error = $"Entry {index + 1} must contain Key,Type,Value. {error}".TrimEnd();
                return false;
            }

            key = key.Trim();
            typeText = typeText.Trim();
            if (key.Length == 0 || !keys.Add(key))
            {
                values = [];
                error = key.Length == 0
                    ? $"Entry {index + 1} has an empty key."
                    : $"Entry {index + 1} repeats key '{key}'.";
                return false;
            }
            if (!TryNormalize(typeText, value, out var type, out var normalized, out error))
            {
                values = [];
                error = $"Entry {index + 1}: {error}";
                return false;
            }
            result.Add(new TaskBlackboardInjectionValue(key, type, normalized));
        }

        values = result;
        error = string.Empty;
        return true;
    }

    public static string Serialize(IEnumerable<TaskBlackboardInjectionValue> values)
    {
        var text = SerializeUnchecked(values);
        if (!TryParse(text, out var normalized, out var error))
            throw new FormatException(error);
        return SerializeUnchecked(normalized);
    }

    private static string SerializeUnchecked(IEnumerable<TaskBlackboardInjectionValue> values) =>
        string.Join(';', values.Select(value =>
            $"{Escape(value.Key)},{GetTypeName(value.Type)},{Escape(value.Value)}"));

    private static bool TryNormalize(
        string typeText,
        string value,
        out TaskBlackboardInjectionValueType type,
        out string normalized,
        out string error)
    {
        switch (typeText.ToLowerInvariant())
        {
            case "bool" when value is "0" or "1":
                type = TaskBlackboardInjectionValueType.Bool;
                normalized = value == "1" ? "true" : "false";
                break;
            case "bool" when bool.TryParse(value, out var boolean):
                type = TaskBlackboardInjectionValueType.Bool;
                normalized = boolean ? "true" : "false";
                break;
            case "int" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer):
                type = TaskBlackboardInjectionValueType.Int;
                normalized = integer.ToString(CultureInfo.InvariantCulture);
                break;
            case "long" when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longInteger):
                type = TaskBlackboardInjectionValueType.Long;
                normalized = longInteger.ToString(CultureInfo.InvariantCulture);
                break;
            case "float" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var single) && float.IsFinite(single):
                type = TaskBlackboardInjectionValueType.Float;
                normalized = single.ToString("R", CultureInfo.InvariantCulture);
                break;
            case "double" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number):
                type = TaskBlackboardInjectionValueType.Double;
                normalized = number.ToString("R", CultureInfo.InvariantCulture);
                break;
            case "string":
                type = TaskBlackboardInjectionValueType.String;
                normalized = value;
                break;
            default:
                type = default;
                normalized = string.Empty;
                error = $"'{value}' is invalid for type '{typeText}'. Supported types: bool, int, long, float, double, string.";
                return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TrySplitEscaped(string text, char delimiter, int maximumParts, out List<string> parts, out string error)
    {
        parts = [];
        var field = new StringBuilder();
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '\\')
            {
                if (++index >= text.Length)
                {
                    error = "A trailing backslash is not a valid escape.";
                    return false;
                }
                field.Append('\\').Append(text[index]);
            }
            else if (character == delimiter && parts.Count < maximumParts - 1)
            {
                parts.Add(field.ToString());
                field.Clear();
            }
            else field.Append(character);
        }
        parts.Add(field.ToString());
        error = string.Empty;
        return true;
    }

    private static bool TryUnescape(string text, out string value, out string error)
    {
        var result = new StringBuilder(text.Length);
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '\\')
            {
                result.Append(text[index]);
                continue;
            }
            if (++index >= text.Length || text[index] is not ('\\' or ',' or ';'))
            {
                value = string.Empty;
                error = "Only \\\\, \\, and \\; are valid escapes.";
                return false;
            }
            result.Append(text[index]);
        }
        value = result.ToString();
        error = string.Empty;
        return true;
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace(",", "\\,", StringComparison.Ordinal)
        .Replace(";", "\\;", StringComparison.Ordinal);

    private static string GetTypeName(TaskBlackboardInjectionValueType type) => type switch
    {
        TaskBlackboardInjectionValueType.Bool => "bool",
        TaskBlackboardInjectionValueType.Int => "int",
        TaskBlackboardInjectionValueType.Long => "long",
        TaskBlackboardInjectionValueType.Float => "float",
        TaskBlackboardInjectionValueType.Double => "double",
        TaskBlackboardInjectionValueType.String => "string",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
    };
}
