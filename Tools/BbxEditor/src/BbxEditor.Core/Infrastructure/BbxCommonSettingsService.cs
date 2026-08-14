using System.Text.Json;

namespace BbxEditor.Infrastructure;

public sealed class BbxCommonSettings
{
    public string ModelDirectory { get; set; } = string.Empty;
}

public sealed class BbxCommonSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _settingsFile;

    public BbxCommonSettingsService(string? settingsFile = null)
    {
        _settingsFile = settingsFile ?? DefaultSettingsFilePath;
    }

    public static string DefaultSettingsFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BbxCommon",
        "settings.json");

    public string SettingsFilePath => _settingsFile;

    public BbxCommonSettings Load()
    {
        if (!File.Exists(_settingsFile))
        {
            var settings = new BbxCommonSettings();
            Save(settings);
            return settings;
        }

        try
        {
            return JsonSerializer.Deserialize<BbxCommonSettings>(File.ReadAllText(_settingsFile), JsonOptions)
                   ?? new BbxCommonSettings();
        }
        catch (JsonException)
        {
            return new BbxCommonSettings();
        }
    }

    public void Save(BbxCommonSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        AtomicFile.WriteAllText(_settingsFile, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
