using System.Globalization;
using BbxEditor.Contracts;

namespace BbxEditor.Wpf.Views;

internal static class CsvInspectorValueCodec
{
    internal static int GetVectorComponentCount(EditorValueKind kind) => kind switch
    {
        EditorValueKind.Vector2 => 2,
        EditorValueKind.Vector3 => 3,
        EditorValueKind.Vector4 => 4,
        _ => 0,
    };

    internal static string[] DecodeVector(string value, int componentCount)
    {
        var components = value.Split(';');
        return Enumerable.Range(0, componentCount)
            .Select(index => index < components.Length ? components[index] : string.Empty)
            .ToArray();
    }

    internal static string EncodeVector(IEnumerable<string> components) => string.Join(';', components);

    internal static bool TryParseColor(string value, out CsvInspectorColor color)
    {
        color = default;
        if ((value.Length != 7 && value.Length != 9) || value[0] != '#') return false;

        if (!byte.TryParse(value.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var red) ||
            !byte.TryParse(value.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var green) ||
            !byte.TryParse(value.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var blue))
        {
            return false;
        }

        var hasAlpha = value.Length == 9;
        var alpha = byte.MaxValue;
        if (hasAlpha &&
            !byte.TryParse(value.AsSpan(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out alpha))
        {
            return false;
        }

        color = new CsvInspectorColor(red, green, blue, alpha, hasAlpha);
        return true;
    }

    internal static string WithRgb(string originalValue, byte red, byte green, byte blue)
    {
        var suffix = TryParseColor(originalValue, out var original) && original.HasAlpha
            ? original.Alpha.ToString("X2", CultureInfo.InvariantCulture)
            : string.Empty;
        return $"#{red:X2}{green:X2}{blue:X2}{suffix}";
    }
}

internal readonly record struct CsvInspectorColor(byte Red, byte Green, byte Blue, byte Alpha, bool HasAlpha);
