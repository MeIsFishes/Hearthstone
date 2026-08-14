using System.Text.Json;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;

namespace BbxEditor.Infrastructure;

public sealed class TaskCatalog
{
    private readonly Dictionary<string, TaskDefinition> _taskByName;
    private readonly Dictionary<string, TaskContextDefinition> _contextByName;
    private readonly Dictionary<string, TaskContextDefinition> _contextByShortName;
    private readonly Dictionary<string, TaskEnumDefinition> _enumByName;

    public TaskCatalog(
        IEnumerable<TaskDefinition> tasks,
        IEnumerable<TaskContextDefinition> contexts,
        IEnumerable<TaskEnumDefinition> enums)
    {
        Tasks = tasks.OrderBy(item => item.TypeName, StringComparer.Ordinal).ToArray();
        Contexts = contexts.OrderBy(item => item.TypeName, StringComparer.Ordinal).ToArray();
        Enums = enums.OrderBy(item => item.TypeName, StringComparer.Ordinal).ToArray();
        _taskByName = Tasks.ToDictionary(item => item.TypeName, StringComparer.Ordinal);
        _contextByName = Contexts.ToDictionary(item => item.TypeName, StringComparer.Ordinal);
        _contextByShortName = Contexts
            .GroupBy(item => item.ShortTypeName, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        _enumByName = Enums.ToDictionary(item => item.TypeName, StringComparer.Ordinal);
    }

    public IReadOnlyList<TaskDefinition> Tasks { get; }
    public IReadOnlyList<TaskContextDefinition> Contexts { get; }
    public IReadOnlyList<TaskEnumDefinition> Enums { get; }

    public TaskDefinition? FindTask(string typeName) => _taskByName.GetValueOrDefault(typeName);
    public TaskContextDefinition? FindContext(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName)) return null;
        if (_contextByName.TryGetValue(typeName, out var exact)) return exact;
        var shortName = typeName[(typeName.LastIndexOf('.') + 1)..];
        return _contextByShortName.GetValueOrDefault(shortName);
    }
    public TaskEnumDefinition? FindEnum(string typeName) => _enumByName.GetValueOrDefault(typeName);

    public static OperationResult<TaskCatalog> LoadFromDirectory(string directory)
    {
        var tasks = new List<TaskDefinition>();
        var contexts = new List<TaskContextDefinition>();
        var enums = new List<TaskEnumDefinition>();
        var result = new OperationResult<TaskCatalog>();

        if (!Directory.Exists(directory))
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CATALOG_DIRECTORY_MISSING", $"The task metadata directory does not exist: {directory}", directory));
            return result;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var root = document.RootElement;
                var fullType = LegacyJson.ReadFullType(root);
                if (fullType?.EndsWith("TaskExportInfo", StringComparison.Ordinal) == true)
                {
                    tasks.Add(ReadTask(root));
                }
                else if (fullType?.EndsWith("TaskContextExportInfo", StringComparison.Ordinal) == true)
                {
                    contexts.Add(ReadContext(root));
                }
                else if (fullType?.EndsWith("TaskEnumExportInfo", StringComparison.Ordinal) == true)
                {
                    enums.Add(ReadEnum(root));
                }
                else if (IsTaskDocument(file, root))
                {
                    // Exported task definitions and editable/runtime task documents may share a
                    // parent directory. Task documents are inputs for the workspace, not metadata.
                    continue;
                }
                else
                {
                    result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Warning, "CATALOG_UNKNOWN_FILE", "Ignored an unrecognized top-level type.", file));
                }
            }
            catch (Exception exception)
            {
                result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CATALOG_READ_FAILED", exception.Message, file));
            }
        }

        AddDuplicateDiagnostics(tasks.Select(item => item.TypeName), "task", result.Diagnostics);
        AddDuplicateDiagnostics(contexts.Select(item => item.TypeName), "Context", result.Diagnostics);
        AddDuplicateDiagnostics(enums.Select(item => item.TypeName), "enum", result.Diagnostics);

        if (result.Diagnostics.All(item => item.Severity != DiagnosticSeverity.Error))
        {
            result.Value = new TaskCatalog(tasks, contexts, enums);
        }
        return result;
    }

    private static bool IsTaskDocument(string file, JsonElement root) =>
        file.EndsWith(".editor.json", StringComparison.OrdinalIgnoreCase) ||
        root.TryGetProperty("TaskInfos", out _) ||
        root.TryGetProperty("NodeEditDataDictionary", out _) ||
        root.TryGetProperty("NodeLineEditDataList", out _);

    private static TaskDefinition ReadTask(JsonElement root)
    {
        var tags = root.TryGetProperty("Tags", out var tagsElement)
            ? LegacyJson.ReadList(tagsElement, element => element.GetString() ?? string.Empty)
            : [];
        var fields = root.TryGetProperty("FieldInfos", out var fieldsElement)
            ? LegacyJson.ReadList(fieldsElement, LegacyJson.ReadFieldDefinition)
            : [];
        return new TaskDefinition(
            LegacyJson.ReadString(root, "TaskTypeName"),
            LegacyJson.ReadString(root, "TaskFullTypeName"),
            LegacyJson.ReadNullableString(root, "Comment"),
            tags,
            fields);
    }

    private static TaskContextDefinition ReadContext(JsonElement root)
    {
        var fields = root.TryGetProperty("FieldInfos", out var fieldsElement)
            ? LegacyJson.ReadList(fieldsElement, LegacyJson.ReadFieldDefinition)
            : [];
        return new TaskContextDefinition(LegacyJson.ReadString(root, "TaskContextTypeName"), fields);
    }

    private static TaskEnumDefinition ReadEnum(JsonElement root)
    {
        var values = root.TryGetProperty("EnumValues", out var valuesElement)
            ? LegacyJson.ReadList(valuesElement, element => element.GetString() ?? string.Empty)
            : [];
        return new TaskEnumDefinition(LegacyJson.ReadString(root, "EnumTypeName"), values);
    }

    private static void AddDuplicateDiagnostics(IEnumerable<string> names, string kind, ICollection<Diagnostic> diagnostics)
    {
        foreach (var duplicate in names.GroupBy(name => name, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "CATALOG_DUPLICATE_NAME", $"Duplicate {kind} name: {duplicate.Key}"));
        }
    }
}
