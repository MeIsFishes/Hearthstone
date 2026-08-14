using System.Text.Json;
using System.Text.Json.Serialization;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;

namespace BbxEditor.Infrastructure;

public sealed class BbxMetadataCatalog
{
    private readonly Dictionary<string, CsvTypeMetadata> _csvByTableName;
    private readonly Dictionary<string, ScriptableObjectTypeMetadata> _scriptableObjectByGuid;

    public BbxMetadataCatalog(
        IEnumerable<CsvTypeMetadata> csvTypes,
        IEnumerable<ScriptableObjectTypeMetadata> scriptableObjectTypes,
        IEnumerable<UnityAssetMetadata> assets)
    {
        CsvTypes = csvTypes.OrderBy(item => item.FullTypeName, StringComparer.Ordinal).ToArray();
        ScriptableObjectTypes = scriptableObjectTypes.OrderBy(item => item.FullTypeName, StringComparer.Ordinal).ToArray();
        Assets = assets.OrderBy(item => item.AssetPath, StringComparer.OrdinalIgnoreCase).ToArray();
        _csvByTableName = CsvTypes
            .SelectMany(type => type.TableNames.Append(type.TypeName).Where(name => !string.IsNullOrWhiteSpace(name)).Select(name => (name, type)))
            .GroupBy(item => item.name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(item => item.type.FullTypeName).Distinct(StringComparer.Ordinal).Count() == 1)
            .ToDictionary(group => group.Key, group => group.First().type, StringComparer.OrdinalIgnoreCase);
        _scriptableObjectByGuid = ScriptableObjectTypes
            .Where(item => !string.IsNullOrWhiteSpace(item.ScriptGuid))
            .GroupBy(item => item.ScriptGuid, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);
    }

    public static BbxMetadataCatalog Empty { get; } = new([], [], []);
    public IReadOnlyList<CsvTypeMetadata> CsvTypes { get; }
    public IReadOnlyList<ScriptableObjectTypeMetadata> ScriptableObjectTypes { get; }
    public IReadOnlyList<UnityAssetMetadata> Assets { get; }

    public CsvTypeMetadata? FindCsvForPath(string path) =>
        FindCsvByTableName(Path.GetFileNameWithoutExtension(path));

    public CsvTypeMetadata? FindCsvByTableName(string tableName) =>
        string.IsNullOrWhiteSpace(tableName) ? null : _csvByTableName.GetValueOrDefault(tableName);

    public ScriptableObjectTypeMetadata? FindScriptableObjectByGuid(string guid) =>
        string.IsNullOrWhiteSpace(guid) ? null : _scriptableObjectByGuid.GetValueOrDefault(guid);

    public static OperationResult<BbxMetadataCatalog> LoadFromDirectory(string directory)
    {
        var result = new OperationResult<BbxMetadataCatalog>();
        if (!Directory.Exists(directory))
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "METADATA_DIRECTORY_MISSING", $"The BbxEditor metadata directory does not exist: {directory}", directory));
            return result;
        }

        var csv = ReadMetadataFiles<CsvTypeMetadata>(Path.Combine(directory, "Csv"), result.Diagnostics);
        var scriptableObjects = ReadMetadataFiles<ScriptableObjectTypeMetadata>(Path.Combine(directory, "ScriptableObject"), result.Diagnostics);
        var assets = ReadAssetIndex(Path.Combine(directory, "Assets", "asset-index.json"), result.Diagnostics);
        result.Value = new BbxMetadataCatalog(csv, scriptableObjects, assets);
        return result;
    }

    private static List<T> ReadMetadataFiles<T>(string directory, ICollection<Diagnostic> diagnostics)
    {
        var values = new List<T>();
        if (!Directory.Exists(directory)) return values;
        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
        {
            try
            {
                var item = JsonSerializer.Deserialize<T>(File.ReadAllText(file), JsonOptions);
                if (item is not null) values.Add(item);
            }
            catch (Exception exception)
            {
                diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "METADATA_READ_FAILED", exception.Message, file));
            }
        }
        return values;
    }

    private static List<UnityAssetMetadata> ReadAssetIndex(string file, ICollection<Diagnostic> diagnostics)
    {
        if (!File.Exists(file)) return [];
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var element = document.RootElement;
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("assets", out var assets) || element.TryGetProperty("Assets", out assets)) element = assets;
            }
            return JsonSerializer.Deserialize<List<UnityAssetMetadata>>(element.GetRawText(), JsonOptions) ?? [];
        }
        catch (Exception exception)
        {
            diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "ASSET_INDEX_READ_FAILED", exception.Message, file));
            return [];
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };
}
