using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;

namespace BbxEditor.Infrastructure;

public static partial class ScriptableObjectDocumentCodec
{
    public static OperationResult<ScriptableObjectDocument> Open(string path, ScriptableObjectTypeMetadata? metadata)
    {
        var result = new OperationResult<ScriptableObjectDocument>();
        if (metadata is null)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "SCRIPTABLE_OBJECT_METADATA_MISSING", "The asset is not recognized as an exported BbxScriptableObject type.", path));
            return result;
        }
        try
        {
            var bytes = File.ReadAllBytes(path);
            var hasBom = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble);
            var content = Encoding.UTF8.GetString(bytes, hasBom ? Encoding.UTF8.Preamble.Length : 0, bytes.Length - (hasBom ? Encoding.UTF8.Preamble.Length : 0));
            var newLine = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            var lines = content.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();
            var guid = ReadScriptGuid(content);
            if (!string.Equals(guid, metadata.ScriptGuid, StringComparison.OrdinalIgnoreCase))
            {
                result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "SCRIPTABLE_OBJECT_GUID_MISMATCH", "The asset script GUID does not match its exported metadata.", path));
                return result;
            }

            var document = new ScriptableObjectDocument
            {
                FilePath = Path.GetFullPath(path),
                ScriptGuid = guid,
                Metadata = metadata,
                NewLine = newLine,
                HasUtf8Bom = hasBom,
            };
            document.SourceLines.AddRange(lines);
            var metadataByPath = Flatten(metadata.Fields).ToDictionary(item => item.Path, item => item.Field, StringComparer.Ordinal);
            var parents = new List<(int Indent, string Path)>();
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (!TryReadKey(line, out var indent, out var key, out var valueStart, out var rawValue) || key.StartsWith("m_", StringComparison.Ordinal)) continue;
                if (key == "MonoBehaviour") continue;
                while (parents.Count > 0 && parents[^1].Indent >= indent) parents.RemoveAt(parents.Count - 1);
                var propertyPath = parents.Count == 0 ? key : parents[^1].Path + "." + key;
                if (!metadataByPath.TryGetValue(propertyPath, out var field))
                {
                    if (rawValue.Length == 0) parents.Add((indent, propertyPath));
                    continue;
                }

                if (field.Type.Kind == EditorValueKind.Array && rawValue.Length == 0)
                {
                    var values = new List<string>();
                    var end = index;
                    while (end + 1 < lines.Count && TryReadSequenceItem(lines[end + 1], indent, out var item))
                    {
                        end++;
                        values.Add(DecodeScalar(item));
                    }
                    document.Properties.Add(CreateProperty(field, propertyPath, string.Join(Environment.NewLine, values.Select(value => DecodeTyped(value, field.Type.ElementType))), index, end, -1, true));
                    index = end;
                }
                else if (rawValue.Length > 0)
                {
                    document.Properties.Add(CreateProperty(field, propertyPath, DecodeTyped(rawValue, field.Type), index, index, valueStart, false));
                }
                else
                {
                    parents.Add((indent, propertyPath));
                }
            }
            document.EnableChangeTracking();
            document.IsDirty = false;
            result.Diagnostics.AddRange(Validate(document));
            result.Value = document;
        }
        catch (Exception exception)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "SCRIPTABLE_OBJECT_OPEN_FAILED", exception.Message, path));
        }
        return result;
    }

    public static OperationResult<string> Save(ScriptableObjectDocument document, string path)
    {
        var result = new OperationResult<string>();
        result.Diagnostics.AddRange(Validate(document));
        if (result.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error)) return result;
        try
        {
            var lines = document.SourceLines.ToList();
            foreach (var property in document.Properties.OrderByDescending(item => item.LineIndex))
            {
                if (property.IsReadOnly) continue;
                if (property.IsSequence)
                {
                    var indent = new string(' ', CountIndent(lines[property.LineIndex]));
                    var oldItemCount = property.LineEndIndex - property.LineIndex;
                    if (oldItemCount > 0) lines.RemoveRange(property.LineIndex + 1, oldItemCount);
                    var values = property.Value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    lines.InsertRange(property.LineIndex + 1, values.Select(value => indent + "- " + EncodeTyped(value, property.Type.ElementType)));
                    var delta = values.Length - oldItemCount;
                    if (delta != 0)
                    {
                        foreach (var following in document.Properties.Where(item => !ReferenceEquals(item, property) && item.LineIndex > property.LineEndIndex))
                        {
                            following.LineIndex += delta;
                            following.LineEndIndex += delta;
                        }
                    }
                    property.LineEndIndex = property.LineIndex + values.Length;
                }
                else
                {
                    lines[property.LineIndex] = lines[property.LineIndex][..property.ValueStart] + EncodeTyped(property.Value, property.Type);
                }
            }
            var fullPath = Path.GetFullPath(path);
            AtomicFile.WriteAllText(fullPath, string.Join(document.NewLine, lines), document.HasUtf8Bom);
            document.SourceLines.Clear();
            document.SourceLines.AddRange(lines);
            document.FilePath = fullPath;
            document.IsDirty = false;
            result.Value = fullPath;
        }
        catch (Exception exception)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "SCRIPTABLE_OBJECT_SAVE_FAILED", exception.Message, path));
        }
        return result;
    }

    public static IReadOnlyList<Diagnostic> Validate(ScriptableObjectDocument document)
    {
        var diagnostics = new List<Diagnostic>();
        foreach (var property in document.Properties.Where(item => !item.IsReadOnly))
        {
            if (property.Type.Kind == EditorValueKind.Array) continue;
            if (!IsValid(property.Value, property.Type))
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "SCRIPTABLE_OBJECT_VALUE_INVALID", $"{property.Path}: '{property.Value}' is not valid for {property.Type.Kind}.", document.FilePath));
            }
        }
        return diagnostics;
    }

    public static string ReadScriptGuid(string content) => ScriptGuidRegex().Match(content) is { Success: true } match ? match.Groups[1].Value : string.Empty;

    private static ScriptableObjectProperty CreateProperty(EditorFieldMetadata field, string path, string value, int line, int end, int valueStart, bool sequence) => new()
    {
        Path = path,
        DisplayName = string.IsNullOrWhiteSpace(field.DisplayName) ? field.Name : field.DisplayName,
        Tooltip = field.Tooltip,
        Type = field.Type,
        IsReadOnly = field.ReadOnly,
        LineIndex = line,
        LineEndIndex = end,
        ValueStart = valueStart,
        IsSequence = sequence,
        Value = value,
    };

    private static IEnumerable<(string Path, EditorFieldMetadata Field)> Flatten(IEnumerable<EditorFieldMetadata> fields, string prefix = "")
    {
        foreach (var field in fields)
        {
            var path = string.IsNullOrEmpty(prefix) ? field.Name : prefix + "." + field.Name;
            yield return (path, field);
            foreach (var nested in Flatten(field.Fields, path)) yield return nested;
        }
    }

    private static bool TryReadKey(string line, out int indent, out string key, out int valueStart, out string value)
    {
        indent = CountIndent(line); key = string.Empty; valueStart = -1; value = string.Empty;
        var trimmed = line.AsSpan(indent);
        if (trimmed.IsEmpty || trimmed[0] == '-' || trimmed[0] == '#') return false;
        var colon = trimmed.IndexOf(':');
        if (colon <= 0) return false;
        key = trimmed[..colon].ToString();
        valueStart = indent + colon + 1;
        while (valueStart < line.Length && line[valueStart] == ' ') valueStart++;
        value = line[valueStart..];
        return true;
    }

    private static bool TryReadSequenceItem(string line, int expectedIndent, out string value)
    {
        var indent = CountIndent(line);
        var trimmed = line.AsSpan(indent);
        if (indent != expectedIndent || trimmed.Length == 0 || trimmed[0] != '-') { value = string.Empty; return false; }
        value = trimmed[1..].TrimStart().ToString();
        return true;
    }

    private static int CountIndent(string line)
    {
        var count = 0;
        while (count < line.Length && line[count] == ' ') count++;
        return count;
    }

    private static bool IsValid(string value, EditorTypeMetadata type) => type.Kind switch
    {
        EditorValueKind.Boolean => value is "0" or "1" || bool.TryParse(value, out _),
        EditorValueKind.Int32 => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Int64 => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.UInt32 => uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.UInt64 => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Single => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Double => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Decimal => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Enum => type.EnumValues.Count == 0 || type.EnumValues.Contains(value, StringComparer.OrdinalIgnoreCase) || int.TryParse(value, out _),
        _ => true,
    };

    private static string DecodeScalar(string value)
    {
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"') return value[1..^1].Replace("\\\"", "\"").Replace("\\n", "\n");
        return value;
    }

    private static string DecodeTyped(string value, EditorTypeMetadata? type)
    {
        var scalar = DecodeScalar(value);
        if (type?.Kind == EditorValueKind.Boolean) return scalar == "1" ? "true" : scalar == "0" ? "false" : scalar;
        if (type?.Kind == EditorValueKind.Enum && long.TryParse(scalar, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            var named = type.EnumNumericValues.FirstOrDefault(item => item.Value == numeric).Key;
            if (!string.IsNullOrEmpty(named)) return named;
            if (numeric >= 0 && numeric < type.EnumValues.Count) return type.EnumValues[(int)numeric];
        }
        return scalar;
    }

    private static string EncodeTyped(string value, EditorTypeMetadata? type)
    {
        if (type?.Kind == EditorValueKind.Boolean && bool.TryParse(value, out var boolean)) return boolean ? "1" : "0";
        if (type?.Kind == EditorValueKind.Enum)
        {
            if (type.EnumNumericValues.TryGetValue(value, out var numeric)) return numeric.ToString(CultureInfo.InvariantCulture);
            var index = type.EnumValues.FindIndex(item => string.Equals(item, value, StringComparison.Ordinal));
            if (index >= 0) return index.ToString(CultureInfo.InvariantCulture);
        }
        if (type?.Kind == EditorValueKind.UnityObjectReference && value.StartsWith('{') && value.EndsWith('}')) return value;
        return EncodeScalar(value);
    }

    private static string EncodeScalar(string value)
    {
        if (value.Length == 0) return "''";
        if (value.Any(character => character is ':' or '#' or '{' or '}' or '[' or ']' or ',' or '"' or '\r' or '\n') || char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", string.Empty).Replace("\n", "\\n") + "\"";
        return value;
    }

    [GeneratedRegex(@"m_Script:\s*\{[^}]*guid:\s*([0-9a-fA-F]{32})[^}]*\}", RegexOptions.CultureInvariant)]
    private static partial Regex ScriptGuidRegex();
}
