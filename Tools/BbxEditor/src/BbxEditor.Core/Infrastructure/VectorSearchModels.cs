using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BbxEditor.Infrastructure;

public static partial class VectorSearchNameNormalizer
{
    private static readonly string[] FileSuffixes =
    [
        ".editor.json", ".csv", ".asset", ".json", ".yaml", ".yml",
    ];

    private static readonly string[] SemanticSuffixes =
    [
        "CsvData", "ScriptableObjectData", "ScriptableObject", "EditorData", "TaskData", "Data", "Asset", "Task",
    ];

    private static readonly string[] SemanticPrefixes =
    [
        "TaskCondition", "TaskDuration", "TaskTimeline", "TaskNode", "TaskOnce", "TaskBt", "Task",
    ];

    public static string NormalizeFileName(string path)
    {
        var name = Path.GetFileName(path).Trim();
        foreach (var suffix in FileSuffixes.OrderByDescending(item => item.Length))
        {
            if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
            name = name[..^suffix.Length];
            break;
        }

        return NormalizeSemanticName(name);
    }

    public static string NormalizeTaskName(string taskTypeName)
    {
        var separator = taskTypeName.LastIndexOf('.');
        var shortName = (separator >= 0 ? taskTypeName[(separator + 1)..] : taskTypeName).Trim();
        return NormalizeSemanticName(shortName);
    }

    private static string NormalizeSemanticName(string name)
    {
        var changed = true;
        while (changed && name.Length > 0)
        {
            changed = false;
            foreach (var prefix in SemanticPrefixes.OrderByDescending(item => item.Length))
            {
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || name.Length == prefix.Length) continue;
                name = name[prefix.Length..].Trim('_', '-', ' ');
                changed = true;
                break;
            }

            foreach (var suffix in SemanticSuffixes.OrderByDescending(item => item.Length))
            {
                if (!name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) || name.Length == suffix.Length) continue;
                name = name[..^suffix.Length].Trim('_', '-', ' ');
                changed = true;
                break;
            }
        }

        return ToReadableText(name);
    }

    public static string NormalizeQuery(string query) => ToReadableText(query.Trim());

    private static string ToReadableText(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = SeparatorRegex().Replace(value, " ");
        text = AcronymBoundaryRegex().Replace(text, "$1 $2");
        text = CamelBoundaryRegex().Replace(text, "$1 $2");
        text = LetterDigitBoundaryRegex().Replace(text, "$1 $2");
        text = DigitLetterBoundaryRegex().Replace(text, "$1 $2");
        return WhitespaceRegex().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"[_\-.]+", RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"([\p{Lu}]+)([\p{Lu}][\p{Ll}])", RegexOptions.CultureInvariant)]
    private static partial Regex AcronymBoundaryRegex();

    [GeneratedRegex(@"([\p{Ll}\p{Lo}])([\p{Lu}])", RegexOptions.CultureInvariant)]
    private static partial Regex CamelBoundaryRegex();

    [GeneratedRegex(@"([\p{L}])(\p{N})", RegexOptions.CultureInvariant)]
    private static partial Regex LetterDigitBoundaryRegex();

    [GeneratedRegex(@"(\p{N})([\p{L}])", RegexOptions.CultureInvariant)]
    private static partial Regex DigitLetterBoundaryRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();
}

public sealed class VectorIndexCache
{
    public int SchemaVersion { get; set; } = 1;
    public string ModelFingerprint { get; set; } = string.Empty;
    public int Dimension { get; set; }
    public Dictionary<string, float[]> Vectors { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class VectorIndexCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _cacheFile;

    public VectorIndexCacheStore(string cacheFile)
    {
        _cacheFile = cacheFile;
    }

    public string CacheFilePath => _cacheFile;

    public VectorIndexCache Load()
    {
        if (!File.Exists(_cacheFile)) return new VectorIndexCache();
        try
        {
            var cache = JsonSerializer.Deserialize<VectorIndexCache>(File.ReadAllText(_cacheFile), JsonOptions)
                        ?? new VectorIndexCache();
            cache.Vectors = new Dictionary<string, float[]>(cache.Vectors ?? [], StringComparer.OrdinalIgnoreCase);
            return cache;
        }
        catch (JsonException)
        {
            return new VectorIndexCache();
        }
    }

    public void Save(VectorIndexCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        AtomicFile.WriteAllText(_cacheFile, JsonSerializer.Serialize(cache, JsonOptions));
    }
}

public sealed class CsvVectorIndexCache
{
    public int SchemaVersion { get; set; } = 1;
    public string ModelFingerprint { get; set; } = string.Empty;
    public Dictionary<string, VectorIndexCache> Columns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class CsvVectorIndexCacheStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _cacheFile;

    public CsvVectorIndexCacheStore(string cacheFile)
    {
        _cacheFile = cacheFile;
    }

    public string CacheFilePath => _cacheFile;

    public CsvVectorIndexCache Load()
    {
        if (!File.Exists(_cacheFile)) return new CsvVectorIndexCache();
        try
        {
            var cache = JsonSerializer.Deserialize<CsvVectorIndexCache>(File.ReadAllText(_cacheFile), JsonOptions)
                        ?? new CsvVectorIndexCache();
            cache.Columns = new Dictionary<string, VectorIndexCache>(cache.Columns ?? [], StringComparer.OrdinalIgnoreCase);
            foreach (var column in cache.Columns.Values)
                column.Vectors = new Dictionary<string, float[]>(column.Vectors ?? [], StringComparer.OrdinalIgnoreCase);
            return cache;
        }
        catch (JsonException)
        {
            return new CsvVectorIndexCache();
        }
    }

    public void Save(CsvVectorIndexCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        AtomicFile.WriteAllText(_cacheFile, JsonSerializer.Serialize(cache, JsonOptions));
    }
}

public static class EmbeddingModelLayout
{
    public const string ModelFolderName = "paraphrase-multilingual-mpnet-base-v2-quint8-avx2";
    public const string ModelFileName = "model_quint8_avx2.onnx";
    public const string TokenizerFileName = "tokenizer.json";

    public static string? ResolveModelDirectory(string configuredDirectory)
    {
        if (string.IsNullOrWhiteSpace(configuredDirectory)) return null;
        string root;
        try
        {
            root = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configuredDirectory.Trim()));
        }
        catch (Exception) when (configuredDirectory.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return null;
        }

        if (HasRequiredFiles(root)) return root;
        var child = Path.Combine(root, ModelFolderName);
        return HasRequiredFiles(child) ? child : null;
    }

    public static string CreateFingerprint(string modelDirectory)
    {
        var model = new FileInfo(Path.Combine(modelDirectory, ModelFileName));
        var tokenizer = new FileInfo(Path.Combine(modelDirectory, TokenizerFileName));
        return string.Join('|', Path.GetFullPath(modelDirectory).ToUpperInvariant(), model.Length, model.LastWriteTimeUtc.Ticks,
            tokenizer.Length, tokenizer.LastWriteTimeUtc.Ticks);
    }

    private static bool HasRequiredFiles(string directory) =>
        Directory.Exists(directory) &&
        File.Exists(Path.Combine(directory, ModelFileName)) &&
        File.Exists(Path.Combine(directory, TokenizerFileName));
}
