using System.Text.Json;

namespace BbxEditor.Infrastructure;

public sealed class AppSettings
{
    public static readonly string[] DefaultExplorerDirectories = ["Assets/Resources", "Mods"];

    public string GameProjectPath { get; set; } = string.Empty;
    public string MetadataPath { get; set; } = string.Empty;
    public string LastDocumentPath { get; set; } = string.Empty;
    public List<string> RecentDocumentPaths { get; set; } = [];
    public List<string> ExplorerDirectories { get; set; } = [.. DefaultExplorerDirectories];
    public bool VectorSearchEnabled { get; set; }

    public void RecordRecentDocument(string path, int maxCount = 10)
    {
        if (string.IsNullOrWhiteSpace(path) || maxCount <= 0) return;
        var fullPath = Path.GetFullPath(path);
        RecentDocumentPaths ??= [];
        RecentDocumentPaths.RemoveAll(item => string.Equals(item, fullPath, StringComparison.OrdinalIgnoreCase));
        RecentDocumentPaths.Insert(0, fullPath);
        if (RecentDocumentPaths.Count > maxCount) RecentDocumentPaths.RemoveRange(maxCount, RecentDocumentPaths.Count - maxCount);
        LastDocumentPath = fullPath;
    }

    public void RemoveRecentDocument(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        RecentDocumentPaths ??= [];
        RecentDocumentPaths.RemoveAll(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
    }

    public void NormalizeRecentDocuments(int maxCount = 10)
    {
        var normalized = new List<string>();
        foreach (var path in RecentDocumentPaths ?? [])
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!normalized.Contains(fullPath, StringComparer.OrdinalIgnoreCase)) normalized.Add(fullPath);
            }
            catch (Exception)
            {
                // Ignore malformed persisted paths while retaining temporarily unavailable valid paths.
            }
            if (normalized.Count >= maxCount) break;
        }
        if (normalized.Count == 0 && File.Exists(LastDocumentPath)) normalized.Add(Path.GetFullPath(LastDocumentPath));
        RecentDocumentPaths = normalized;
    }

    public void NormalizeExplorerDirectories()
    {
        ExplorerDirectories = (ExplorerDirectories ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim().Replace('\\', '/').TrimEnd('/'))
            .Where(path => path.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (ExplorerDirectories.Count == 0) ExplorerDirectories.AddRange(DefaultExplorerDirectories);
    }
}

public sealed class SettingsService
{
    private readonly string _settingsFile;

    public SettingsService(string settingsFile)
    {
        _settingsFile = settingsFile;
    }

    public AppSettings Load(string? legacySettingsFile = null)
    {
        if (File.Exists(_settingsFile))
        {
            var json = File.ReadAllText(_settingsFile);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            if (string.IsNullOrWhiteSpace(settings.MetadataPath))
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("exportInfoPath", out var legacyPath)) settings.MetadataPath = legacyPath.GetString() ?? string.Empty;
            }
            settings.NormalizeRecentDocuments();
            settings.NormalizeExplorerDirectories();
            return settings;
        }

        if (!string.IsNullOrWhiteSpace(legacySettingsFile) && File.Exists(legacySettingsFile))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(legacySettingsFile));
            var settings = new AppSettings
            {
                MetadataPath = LegacyJson.ReadString(document.RootElement, "m_ExportInfoPath"),
                LastDocumentPath = LegacyJson.ReadString(document.RootElement, "m_LastSaveTargetPath"),
            };
            settings.NormalizeRecentDocuments();
            settings.NormalizeExplorerDirectories();
            return settings;
        }

        return new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        settings.NormalizeRecentDocuments();
        settings.NormalizeExplorerDirectories();
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile) ?? ".");
        File.WriteAllText(_settingsFile, JsonSerializer.Serialize(settings, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };
}
