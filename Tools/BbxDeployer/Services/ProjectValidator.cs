using System.Text.Json;
using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class ProjectValidator(ProjectLocator projectLocator)
{
    private static readonly IReadOnlyDictionary<string, string> RequiredPackages =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["com.unity.entities"] = "1.0.0-pre.65",
            ["com.unity.textmeshpro"] = "3.0.6",
            ["com.unity.ugui"] = "1.0.0"
        };

    public void ValidateTarget(
        ProjectContext source,
        TargetPreview targetPreview,
        IReadOnlyCollection<SyncItem> enabledItems)
    {
        var target = targetPreview.Target;

        if (!Directory.Exists(target.RepositoryRoot))
        {
            targetPreview.Errors.Add($"Destination repository does not exist: {target.RepositoryRoot}");
            return;
        }

        if (PathService.Overlaps(source.RepositoryRoot, target.RepositoryRoot))
        {
            targetPreview.Errors.Add("Source and destination repositories must not overlap.");
        }

        if (!projectLocator.IsUnityProject(target.UnityProjectRoot)
            && !targetPreview.RequiresUnityBootstrap
            && !targetPreview.RequiresUnityProjectCreation)
        {
            targetPreview.Errors.Add(
                $"Destination is not a valid or bootstrap-compatible Unity project: "
                + target.UnityProjectRoot);
            return;
        }

        if (!enabledItems.Any(item => item.Id == "bbxcommon-source"))
        {
            return;
        }

        if (File.Exists(Path.Combine(
                target.UnityProjectRoot,
                "ProjectSettings",
                "ProjectVersion.txt")))
        {
            ValidateUnityVersion(source, targetPreview);
        }

        if (File.Exists(Path.Combine(
                target.UnityProjectRoot,
                "Packages",
                "manifest.json")))
        {
            ValidatePackages(targetPreview);
        }

        var copiesOdin = enabledItems.Any(item => item.Id == "odin-inspector");
        var targetHasOdin = Directory.Exists(
            Path.Combine(target.UnityProjectRoot, "Assets", "Plugins", "Sirenix"));
        if (!copiesOdin && !targetHasOdin)
        {
            targetPreview.Errors.Add(
                "BbxCommon requires Odin Inspector. Select the Odin Inspector sync item or install it in the destination.");
        }
    }

    public void ValidateCapacity(TargetPreview targetPreview)
    {
        try
        {
            var destinationRoot = Path.GetPathRoot(
                PathService.NormalizeAbsolute(targetPreview.Target.RepositoryRoot));
            if (string.IsNullOrWhiteSpace(destinationRoot))
            {
                return;
            }

            var drive = new DriveInfo(destinationRoot);
            if (drive.AvailableFreeSpace < targetPreview.TotalBytes)
            {
                targetPreview.Errors.Add(
                    $"Insufficient free space. Required {FormatBytes(targetPreview.TotalBytes)}, "
                    + $"available {FormatBytes(drive.AvailableFreeSpace)}.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            targetPreview.Warnings.Add($"Free-space check failed: {exception.Message}");
        }
    }

    private void ValidateUnityVersion(
        ProjectContext source,
        TargetPreview targetPreview)
    {
        var sourceVersion = projectLocator.ReadUnityVersion(source.UnityProjectRoot);
        var targetVersion = projectLocator.ReadUnityVersion(targetPreview.Target.UnityProjectRoot);

        if (sourceVersion is null || targetVersion is null)
        {
            targetPreview.Errors.Add("Cannot read the Unity project version.");
            return;
        }

        var sourceMajor = sourceVersion.Split('.')[0];
        var targetMajor = targetVersion.Split('.')[0];
        if (!sourceMajor.Equals(targetMajor, StringComparison.OrdinalIgnoreCase))
        {
            targetPreview.Errors.Add(
                $"Unity major version mismatch. Source {sourceVersion}, destination {targetVersion}.");
        }
        else if (!sourceVersion.Equals(targetVersion, StringComparison.OrdinalIgnoreCase))
        {
            targetPreview.Warnings.Add(
                $"Unity version differs. Source {sourceVersion}, destination {targetVersion}; validate in the destination Editor.");
        }
    }

    private static void ValidatePackages(TargetPreview targetPreview)
    {
        var manifestPath = Path.Combine(
            targetPreview.Target.UnityProjectRoot,
            "Packages",
            "manifest.json");

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!document.RootElement.TryGetProperty("dependencies", out var dependencies))
            {
                targetPreview.Errors.Add("Packages/manifest.json has no dependencies object.");
                return;
            }

            foreach (var (packageName, expectedVersion) in RequiredPackages)
            {
                if (!dependencies.TryGetProperty(packageName, out var value))
                {
                    targetPreview.Errors.Add(
                        $"Required Unity package is missing: {packageName} {expectedVersion}.");
                    continue;
                }

                var actualVersion = value.GetString();
                if (!expectedVersion.Equals(actualVersion, StringComparison.Ordinal))
                {
                    targetPreview.Errors.Add(
                        $"Unity package version conflict: {packageName} requires {expectedVersion}, found {actualVersion}.");
                }
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            targetPreview.Errors.Add($"Cannot read Packages/manifest.json: {exception.Message}");
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
