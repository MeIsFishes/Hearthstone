using System.Globalization;
using System.Text;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;

namespace BbxEditor.Infrastructure;

public static class CsvDocumentCodec
{
    public static OperationResult<CsvDocument> Open(string path, CsvTypeMetadata? metadata)
    {
        var result = new OperationResult<CsvDocument>();
        try
        {
            var bytes = File.ReadAllBytes(path);
            var hasBom = bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble);
            var content = Encoding.UTF8.GetString(bytes, hasBom ? Encoding.UTF8.Preamble.Length : 0, bytes.Length - (hasBom ? Encoding.UTF8.Preamble.Length : 0));
            var records = Parse(content);
            if (records.Count == 0)
            {
                result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CSV_HEADER_MISSING", "The CSV file does not contain a header row.", path));
                return result;
            }

            var document = new CsvDocument
            {
                FilePath = Path.GetFullPath(path),
                Metadata = metadata,
                NewLine = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n",
                HasUtf8Bom = hasBom,
            };
            foreach (var column in records[0]) document.Columns.Add(column);
            var dataStartIndex = 1;
            while (dataStartIndex < records.Count && records[dataStartIndex].Count > 0 &&
                   records[dataStartIndex][0].StartsWith("//", StringComparison.Ordinal))
            {
                document.HeaderComments.Add(string.Join(',', records[dataStartIndex]));
                dataStartIndex++;
            }
            for (var index = dataStartIndex; index < records.Count; index++)
            {
                if (index == records.Count - 1 && records[index].Count == 1 && records[index][0].Length == 0 && content.EndsWith(document.NewLine, StringComparison.Ordinal)) continue;
                var row = new CsvRow();
                foreach (var value in records[index]) row.Cells.Add(new CsvCell { Value = value });
                while (row.Cells.Count < document.Columns.Count) row.Cells.Add(new CsvCell());
                document.AddRow(row);
            }
            document.EnableChangeTracking();
            document.IsDirty = false;
            result.Diagnostics.AddRange(Validate(document));
            result.Value = document;
        }
        catch (Exception exception)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CSV_OPEN_FAILED", exception.Message, path));
        }
        return result;
    }

    public static OperationResult<string> Save(CsvDocument document, string path)
    {
        var result = new OperationResult<string>();
        result.Diagnostics.AddRange(Validate(document));
        if (result.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error)) return result;
        try
        {
            var fullPath = Path.GetFullPath(path);
            var lines = new List<string> { WriteRecord(document.Columns.ToArray()) };
            lines.AddRange(document.HeaderComments);
            lines.AddRange(document.Rows.Select(row => WriteRecord(row.Cells.Select(cell => cell.Value).ToArray())));
            var content = string.Join(document.NewLine, lines) + document.NewLine;
            AtomicFile.WriteAllText(fullPath, content, document.HasUtf8Bom);
            document.FilePath = fullPath;
            document.IsDirty = false;
            result.Value = fullPath;
        }
        catch (Exception exception)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CSV_SAVE_FAILED", exception.Message, path));
        }
        return result;
    }

    public static IReadOnlyList<Diagnostic> Validate(CsvDocument document)
    {
        var diagnostics = new List<Diagnostic>();
        ValidateHeaderComments(document, diagnostics);
        var duplicateColumns = document.Columns.GroupBy(item => item, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1);
        foreach (var duplicate in duplicateColumns) diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CSV_DUPLICATE_COLUMN", $"Duplicate CSV column: {duplicate.Key}", document.FilePath));
        for (var rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
        {
            if (document.Rows[rowIndex].Cells.Count != document.Columns.Count)
                diagnostics.Add(CellError("CSV_COLUMN_COUNT_MISMATCH", rowIndex, "<row>", $"Expected {document.Columns.Count} cells but found {document.Rows[rowIndex].Cells.Count}.", document));
        }
        if (document.Metadata is null) return diagnostics;

        foreach (var column in document.Metadata.Columns)
        {
            var columnIndex = document.Columns.Select((name, index) => (name, index))
                .Where(item => string.Equals(item.name, column.Name, StringComparison.OrdinalIgnoreCase))
                .Select(item => item.index).DefaultIfEmpty(-1).First();
            if (columnIndex < 0)
            {
                if (column.Required) diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CSV_REQUIRED_COLUMN_MISSING", $"Required CSV column is missing: {column.Name}", document.FilePath));
                continue;
            }
            var seen = column.Unique ? new HashSet<string>(StringComparer.Ordinal) : null;
            for (var rowIndex = 0; rowIndex < document.Rows.Count; rowIndex++)
            {
                var value = columnIndex < document.Rows[rowIndex].Cells.Count ? document.Rows[rowIndex].Cells[columnIndex].Value : string.Empty;
                if (column.Required && string.IsNullOrWhiteSpace(value)) diagnostics.Add(CellError("CSV_REQUIRED_VALUE_MISSING", rowIndex, column.Name, "A required value is empty.", document));
                if (!string.IsNullOrWhiteSpace(value) && !IsValid(value, column.Type)) diagnostics.Add(CellError("CSV_VALUE_INVALID", rowIndex, column.Name, $"The value '{value}' is not valid for {column.Type.Kind}.", document));
                if (seen is not null && !seen.Add(value)) diagnostics.Add(CellError("CSV_DUPLICATE_VALUE", rowIndex, column.Name, $"The unique value '{value}' is duplicated.", document));
            }
        }
        return diagnostics;
    }

    public static IReadOnlyList<string> GetFieldDescriptions(CsvDocument document)
    {
        if (document.HeaderComments.Count == 0) return [];
        var descriptions = document.HeaderComments[0].Split(',', StringSplitOptions.None);
        if (descriptions.Length != document.Columns.Count || descriptions.Length == 0) return [];

        var first = descriptions[0].TrimStart();
        if (!first.StartsWith("//", StringComparison.Ordinal)) return [];
        descriptions[0] = first[2..].Trim();
        for (var index = 1; index < descriptions.Length; index++) descriptions[index] = descriptions[index].Trim();
        return descriptions;
    }

    private static Diagnostic CellError(string code, int row, string column, string message, CsvDocument document) =>
        new(DiagnosticSeverity.Error, code, $"Row {row + document.HeaderComments.Count + 2}, column {column}: {message}", document.FilePath);

    private static void ValidateHeaderComments(CsvDocument document, ICollection<Diagnostic> diagnostics)
    {
        if (document.HeaderComments.Count != 2)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "CSV_HEADER_COMMENTS_INVALID",
                "CSV headers must be followed by exactly two contract comment lines.",
                document.FilePath));
            return;
        }

        var descriptions = GetFieldDescriptions(document);
        if (!document.HeaderComments[0].StartsWith("// ", StringComparison.Ordinal) ||
            descriptions.Count != document.Columns.Count ||
            descriptions.Any(description => string.IsNullOrWhiteSpace(description) ||
                                            description.Any(character => character > 127)))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "CSV_FIELD_COMMENTS_INVALID",
                "The first header comment must contain one non-empty English description per column.",
                document.FilePath));
        }

        var association = document.HeaderComments[1];
        if (!CsvAssociationContract.TryParse(association, out _, out _))
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticSeverity.Error,
                "CSV_ASSOCIATION_COMMENT_INVALID",
                "The second header comment must use '// Associated: TableA, TableB' or '// Associated: None'.",
                document.FilePath));
        }
    }

    private static bool IsValid(string value, EditorTypeMetadata type) => type.Kind switch
    {
        EditorValueKind.Boolean => bool.TryParse(value, out _),
        EditorValueKind.Int32 => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Int64 => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.UInt32 => uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.UInt64 => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Single => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Double => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Decimal => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _),
        EditorValueKind.Color => IsHexColor(value),
        EditorValueKind.Vector2 => IsFloatVector(value, 2),
        EditorValueKind.Vector3 => IsFloatVector(value, 3),
        EditorValueKind.Vector4 => IsFloatVector(value, 4),
        EditorValueKind.TaskBlackboardInjection => TaskBlackboardInjectionCodec.TryParse(value, out _, out _),
        EditorValueKind.Enum => type.EnumValues.Count == 0 || type.EnumValues.Contains(value, StringComparer.OrdinalIgnoreCase),
        EditorValueKind.Array => value.Split(';', StringSplitOptions.RemoveEmptyEntries).All(item => type.ElementType is null || IsValid(item, type.ElementType)),
        _ => true,
    };

    private static bool IsHexColor(string value) =>
        (value.Length == 7 || value.Length == 9) && value[0] == '#' && value.AsSpan(1).ToArray().All(Uri.IsHexDigit);

    private static bool IsFloatVector(string value, int componentCount)
    {
        var components = value.Split(';');
        return components.Length == componentCount &&
               components.All(component => float.TryParse(component, NumberStyles.Float, CultureInfo.InvariantCulture, out _));
    }

    private static string WriteRecord(IReadOnlyList<string> values) => string.Join(',', values.Select(Escape));
    private static string Escape(string value) => value.IndexOfAny([',', '"', '\r', '\n']) >= 0
        ? $"\"{value.Replace("\"", "\"\"")}\""
        : value;

    private static List<List<string>> Parse(string content)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < content.Length; index++)
        {
            var character = content[index];
            if (quoted)
            {
                if (character == '"' && index + 1 < content.Length && content[index + 1] == '"') { field.Append('"'); index++; }
                else if (character == '"') quoted = false;
                else field.Append(character);
            }
            else if (character == '"' && field.Length == 0) quoted = true;
            else if (character == ',') { record.Add(field.ToString()); field.Clear(); }
            else if (character is '\r' or '\n')
            {
                if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n') index++;
                record.Add(field.ToString()); field.Clear(); records.Add(record); record = [];
            }
            else field.Append(character);
        }
        if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
        return records;
    }
}
