using System.Text.Json;
using System.Text.Json.Serialization;
using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class SettingsRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public SettingsRepository(
        string? syncItemsPath = null,
        string? projectsPath = null,
        string? legacySettingsPath = null)
    {
        SyncItemsPath = syncItemsPath ?? Path.Combine(
            AppContext.BaseDirectory,
            "BbxDeployer.sync-items.json");
        ProjectsPath = projectsPath ?? Path.Combine(
            AppContext.BaseDirectory,
            "BbxDeployer.projects.json");
        LegacySettingsPath = legacySettingsPath ?? Path.Combine(
            AppContext.BaseDirectory,
            "BbxDeployer.settings.json");
    }

    public string SyncItemsPath { get; }

    public string ProjectsPath { get; }

    public string LegacySettingsPath { get; }

    public async Task<AppSettings?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var syncItemSettings = await ReadAsync<SyncItemSettings>(
            SyncItemsPath,
            cancellationToken);
        var projectListSettings = await ReadAsync<ProjectListSettings>(
            ProjectsPath,
            cancellationToken);
        AppSettings? legacySettings = null;
        if ((syncItemSettings is null || projectListSettings is null)
            && File.Exists(LegacySettingsPath))
        {
            legacySettings = await ReadAsync<AppSettings>(
                LegacySettingsPath,
                cancellationToken);
        }

        if (syncItemSettings is null
            && projectListSettings is null
            && legacySettings is null)
        {
            return null;
        }

        var settings = new AppSettings
        {
            Source = projectListSettings?.Source ?? legacySettings?.Source,
            Targets = projectListSettings?.Targets ?? legacySettings?.Targets ?? [],
            SyncItems = syncItemSettings?.SyncItems ?? legacySettings?.SyncItems ?? []
        };

        if (legacySettings is not null)
        {
            await SaveAsync(settings, cancellationToken);
        }

        if (File.Exists(LegacySettingsPath)
            && File.Exists(SyncItemsPath)
            && File.Exists(ProjectsPath)
            && !LegacySettingsPath.Equals(
                SyncItemsPath,
                StringComparison.OrdinalIgnoreCase)
            && !LegacySettingsPath.Equals(
                ProjectsPath,
                StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(LegacySettingsPath);
        }

        return settings;
    }

    public async Task SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        await WriteAsync(
            SyncItemsPath,
            new SyncItemSettings { SyncItems = settings.SyncItems },
            cancellationToken);
        await WriteAsync(
            ProjectsPath,
            new ProjectListSettings
            {
                Source = settings.Source,
                Targets = settings.Targets
            },
            cancellationToken);
    }

    private static async Task<T?> ReadAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await JsonSerializer.DeserializeAsync<T>(
            stream,
            SerializerOptions,
            cancellationToken);
    }

    private static async Task WriteAsync<T>(
        string path,
        T value,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        var content = JsonSerializer.SerializeToUtf8Bytes(
            value,
            SerializerOptions);
        if (File.Exists(path))
        {
            var existing = await File.ReadAllBytesAsync(path, cancellationToken);
            if (content.AsSpan().SequenceEqual(existing))
            {
                return;
            }
        }

        var temporaryPath = path + $".{Guid.NewGuid():N}.tmp";

        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             64 * 1024,
                             FileOptions.Asynchronous))
            {
                await stream.WriteAsync(content, cancellationToken);
            }

            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
