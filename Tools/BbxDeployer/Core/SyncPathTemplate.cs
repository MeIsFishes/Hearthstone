namespace BbxDeployer.Core;

public static class SyncPathTemplate
{
    public const string GameProjectToken = "{GameProject}";

    public static string ToProjectRelativePath(SyncItem item)
    {
        var relativePath = item.SourceRelativePath.Replace('\\', '/').Trim('/');
        return item.SourceBase == PathBaseKind.UnityProjectRoot
            ? $"{GameProjectToken}/{relativePath}"
            : relativePath;
    }

    public static void ApplyProjectRelativePath(SyncItem item, string path)
    {
        var normalized = path.Replace('\\', '/').Trim().Trim('/');
        var gamePrefix = GameProjectToken + "/";

        if (normalized.Equals(GameProjectToken, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Select a directory inside {GameProject}.", nameof(path));
        }

        if (normalized.StartsWith(gamePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var relativePath = normalized[gamePrefix.Length..].Trim('/');
            if (relativePath.Length == 0)
            {
                throw new ArgumentException("Select a directory inside {GameProject}.", nameof(path));
            }

            item.SourceBase = PathBaseKind.UnityProjectRoot;
            item.TargetBase = PathBaseKind.UnityProjectRoot;
            item.SourceRelativePath = relativePath;
            item.TargetRelativePath = relativePath;
            return;
        }

        if (normalized.Length == 0 || Path.IsPathRooted(normalized) || normalized.StartsWith(".."))
        {
            throw new ArgumentException(
                "Enter a directory relative to the project root.",
                nameof(path));
        }

        item.SourceBase = PathBaseKind.RepositoryRoot;
        item.TargetBase = PathBaseKind.RepositoryRoot;
        item.SourceRelativePath = normalized;
        item.TargetRelativePath = normalized;
    }
}
