using System.Text.Json;
using System.Text.RegularExpressions;
using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed partial class UnityEditorLocator : IUnityEditorLocator
{
    private readonly string? _hubSettingsRoot;
    private readonly IReadOnlyCollection<string> _additionalRoots;
    private readonly bool _includeDefaultRoots;

    public UnityEditorLocator(
        string? hubSettingsRoot = null,
        IReadOnlyCollection<string>? additionalRoots = null,
        bool includeDefaultRoots = true)
    {
        _hubSettingsRoot = hubSettingsRoot
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "UnityHub");
        _additionalRoots = additionalRoots ?? [];
        _includeDefaultRoots = includeDefaultRoots;
    }

    public IReadOnlyList<UnityEditorInstallation> DiscoverInstalledEditors()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in _additionalRoots)
        {
            AddRoot(roots, root);
        }

        AddHubSecondaryInstallRoot(roots);
        if (_includeDefaultRoots)
        {
            AddRoot(
                roots,
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Unity",
                    "Hub",
                    "Editor"));
            AddRoot(
                roots,
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Unity",
                    "Hub",
                    "Editor"));
        }

        var byVersion = new Dictionary<string, UnityEditorInstallation>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var root in roots)
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            IEnumerable<string> versionDirectories;
            try
            {
                versionDirectories = Directory.EnumerateDirectories(root).ToList();
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var versionDirectory in versionDirectories)
            {
                var executable = Path.Combine(versionDirectory, "Editor", "Unity.exe");
                if (!File.Exists(executable))
                {
                    continue;
                }

                var version = new DirectoryInfo(versionDirectory).Name;
                byVersion.TryAdd(
                    version,
                    new UnityEditorInstallation
                    {
                        Version = version,
                        ExecutablePath = Path.GetFullPath(executable)
                    });
            }
        }

        return byVersion.Values
            .OrderByDescending(editor => editor.Version, UnityVersionComparer.Instance)
            .ToList();
    }

    public UnityEditorInstallation? FindByVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        return DiscoverInstalledEditors().FirstOrDefault(
            editor => editor.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
    }

    private void AddHubSecondaryInstallRoot(ISet<string> roots)
    {
        if (string.IsNullOrWhiteSpace(_hubSettingsRoot))
        {
            return;
        }

        var path = Path.Combine(_hubSettingsRoot, "secondaryInstallPath.json");
        if (!File.Exists(path))
        {
            return;
        }

        try
        {
            var root = JsonSerializer.Deserialize<string>(File.ReadAllText(path));
            AddRoot(roots, root);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A damaged Hub cache must not prevent discovery from the default roots.
        }
    }

    private static void AddRoot(ISet<string> roots, string? root)
    {
        if (!string.IsNullOrWhiteSpace(root))
        {
            roots.Add(Path.GetFullPath(root));
        }
    }

    private sealed partial class UnityVersionComparer : IComparer<string>
    {
        public static UnityVersionComparer Instance { get; } = new();

        public int Compare(string? left, string? right)
        {
            var leftParts = Parse(left);
            var rightParts = Parse(right);
            var result = leftParts.Major.CompareTo(rightParts.Major);
            if (result == 0)
            {
                result = leftParts.Minor.CompareTo(rightParts.Minor);
            }

            if (result == 0)
            {
                result = leftParts.Patch.CompareTo(rightParts.Patch);
            }

            return result != 0
                ? result
                : StringComparer.OrdinalIgnoreCase.Compare(left, right);
        }

        private static (int Major, int Minor, int Patch) Parse(string? version)
        {
            var match = VersionPrefixRegex().Match(version ?? string.Empty);
            return match.Success
                   && int.TryParse(match.Groups[1].Value, out var major)
                   && int.TryParse(match.Groups[2].Value, out var minor)
                   && int.TryParse(match.Groups[3].Value, out var patch)
                ? (major, minor, patch)
                : (0, 0, 0);
        }

        [GeneratedRegex(@"^(\d+)\.(\d+)\.(\d+)")]
        private static partial Regex VersionPrefixRegex();
    }
}
