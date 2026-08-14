using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class ProjectLocator
{
    public bool IsUnityProject(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        return Directory.Exists(Path.Combine(path, "Assets"))
            && File.Exists(Path.Combine(path, "Packages", "manifest.json"))
            && File.Exists(Path.Combine(path, "ProjectSettings", "ProjectVersion.txt"));
    }

    public IReadOnlyList<string> DiscoverUnityProjects(string repositoryRoot)
    {
        if (!Directory.Exists(repositoryRoot))
        {
            return [];
        }

        return Directory.EnumerateDirectories(repositoryRoot)
            .Where(path => !HasReparsePoint(path))
            .Where(IsUnityProject)
            .Select(PathService.NormalizeAbsolute)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string? InferRepositoryRootFromExecutable(string executableBaseDirectory)
    {
        var current = new DirectoryInfo(PathService.NormalizeAbsolute(executableBaseDirectory));

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "BbxDeployer.csproj"))
                && current.Parent?.Name.Equals("Tools", StringComparison.OrdinalIgnoreCase) == true)
            {
                return current.Parent.Parent?.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    public ProjectContext CreateContextFromUnityProject(string unityProjectRoot, string? repositoryRoot = null)
    {
        var normalizedUnityRoot = PathService.NormalizeAbsolute(unityProjectRoot);
        var normalizedRepositoryRoot = string.IsNullOrWhiteSpace(repositoryRoot)
            ? Directory.GetParent(normalizedUnityRoot)?.FullName
                ?? throw new InvalidOperationException("The Unity project must have a parent repository directory.")
            : PathService.NormalizeAbsolute(repositoryRoot);

        return new ProjectContext
        {
            DisplayName = new DirectoryInfo(normalizedUnityRoot).Name,
            RepositoryRoot = normalizedRepositoryRoot,
            UnityProjectRoot = normalizedUnityRoot
        };
    }

    public ProjectContext CreateContextFromProjectRoot(string selectedRoot)
    {
        var normalizedRoot = PathService.NormalizeAbsolute(selectedRoot);
        if (IsUnityProject(normalizedRoot))
        {
            var repositoryRoot = Directory.GetParent(normalizedRoot)?.FullName
                ?? throw new InvalidOperationException(
                    "The Unity project must have a parent project root.");
            return CreateContextFromUnityProject(normalizedRoot, repositoryRoot);
        }

        var candidates = DiscoverUnityProjects(normalizedRoot);
        return candidates.Count switch
        {
            1 => CreateContextFromUnityProject(candidates[0], normalizedRoot),
            0 => throw new InvalidOperationException(
                "No Unity game directory was found directly inside this project root."),
            _ => throw new InvalidOperationException(
                "Multiple Unity game directories were found. Select the game directory itself.")
        };
    }

    public ProjectContext CreateDestinationContextFromProjectRoot(string selectedRoot)
    {
        var normalizedRoot = PathService.NormalizeAbsolute(selectedRoot);
        if (IsUnityProject(normalizedRoot))
        {
            return CreateContextFromProjectRoot(normalizedRoot);
        }

        var candidates = DiscoverUnityProjects(normalizedRoot);
        if (candidates.Count == 1)
        {
            return CreateContextFromUnityProject(candidates[0], normalizedRoot);
        }

        if (candidates.Count > 1)
        {
            throw new InvalidOperationException(
                "Multiple Unity game directories were found. Select the game directory itself.");
        }

        var name = new DirectoryInfo(normalizedRoot).Name;
        return new ProjectContext
        {
            DisplayName = name,
            RepositoryRoot = normalizedRoot,
            UnityProjectRoot = Path.Combine(normalizedRoot, name)
        };
    }

    public bool CanBootstrapUnityProject(ProjectContext context)
    {
        if (!Directory.Exists(context.RepositoryRoot)
            || string.IsNullOrWhiteSpace(context.UnityProjectRoot))
        {
            return false;
        }

        try
        {
            var repositoryRoot = PathService.NormalizeAbsolute(context.RepositoryRoot);
            var unityProjectRoot = PathService.NormalizeAbsolute(context.UnityProjectRoot);
            return Directory.GetParent(unityProjectRoot)?.FullName.Equals(
                repositoryRoot,
                StringComparison.OrdinalIgnoreCase) == true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool CanCreateUnityProject(ProjectContext context)
    {
        if (!CanBootstrapUnityProject(context))
        {
            return false;
        }

        try
        {
            return !Directory.Exists(context.UnityProjectRoot)
                || !Directory.EnumerateFileSystemEntries(context.UnityProjectRoot).Any();
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public string? ReadUnityVersion(string unityProjectRoot)
    {
        var path = Path.Combine(unityProjectRoot, "ProjectSettings", "ProjectVersion.txt");
        if (!File.Exists(path))
        {
            return null;
        }

        foreach (var line in File.ReadLines(path))
        {
            const string prefix = "m_EditorVersion:";
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                return line[prefix.Length..].Trim();
            }
        }

        return null;
    }

    public IReadOnlyList<SyncItem> CreateDefaultSyncItems()
    {
        return
        [
            new SyncItem
            {
                Id = "shared-tools",
                DisplayName = "Shared Tools",
                Enabled = true,
                IsBuiltIn = true,
                SourceBase = PathBaseKind.RepositoryRoot,
                SourceRelativePath = "Tools",
                TargetBase = PathBaseKind.RepositoryRoot,
                TargetRelativePath = "Tools",
                UseGitIgnoreFiles = true
            },
            new SyncItem
            {
                Id = "bbxcommon-source",
                DisplayName = "BbxCommon Source",
                Enabled = true,
                IsBuiltIn = true,
                SourceBase = PathBaseKind.UnityProjectRoot,
                SourceRelativePath = "Assets/Scripts/BbxCommon",
                TargetBase = PathBaseKind.UnityProjectRoot,
                TargetRelativePath = "Assets/Scripts/BbxCommon",
                IncludeCompanionMeta = false,
                UseGitIgnoreFiles = true,
                WhitelistPaths =
                [
                    new SyncPathEntry
                    {
                        RelativePath = "{GameProject}/Assets/Scripts/BbxCommon"
                    },
                    new SyncPathEntry
                    {
                        RelativePath = "{GameProject}/AutoDoc/UIItem"
                    }
                ]
            },
            new SyncItem
            {
                Id = "odin-inspector",
                DisplayName = "Odin Inspector",
                Enabled = true,
                IsBuiltIn = true,
                SourceBase = PathBaseKind.UnityProjectRoot,
                SourceRelativePath = "Assets/Plugins/Sirenix",
                TargetBase = PathBaseKind.UnityProjectRoot,
                TargetRelativePath = "Assets/Plugins/Sirenix",
                IncludeCompanionMeta = false,
                UseGitIgnoreFiles = true
            },
            new SyncItem
            {
                Id = "codex-project-config",
                DisplayName = "Codex Project Configuration",
                Enabled = true,
                IsBuiltIn = true,
                SourceBase = PathBaseKind.UnityProjectRoot,
                SourceRelativePath = ".codex",
                TargetBase = PathBaseKind.UnityProjectRoot,
                TargetRelativePath = ".codex",
                UseGitIgnoreFiles = true,
                WhitelistPaths =
                [
                    new SyncPathEntry
                    {
                        RelativePath = "{GameProject}/.codex"
                    },
                    new SyncPathEntry
                    {
                        RelativePath = "{GameProject}/AGENTS.md"
                    },
                    new SyncPathEntry
                    {
                        RelativePath = "{GameProject}/AutoDoc/CleanupTempDocs.bat"
                    }
                ]
            }
        ];
    }

    private static bool HasReparsePoint(string path)
    {
        return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    }
}
