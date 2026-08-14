using BbxDeployer.Core;

namespace BbxDeployer.Services;

public static class PathService
{
    private static readonly StringComparison PathComparison = StringComparison.OrdinalIgnoreCase;

    public static string NormalizeAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path.Trim()));
    }

    public static string ResolveBase(ProjectContext context, PathBaseKind baseKind)
    {
        return baseKind switch
        {
            PathBaseKind.RepositoryRoot => NormalizeAbsolute(context.RepositoryRoot),
            PathBaseKind.UnityProjectRoot => NormalizeAbsolute(context.UnityProjectRoot),
            _ => throw new ArgumentOutOfRangeException(nameof(baseKind))
        };
    }

    public static string ResolveInside(string basePath, string relativePath)
    {
        var normalizedBase = NormalizeAbsolute(basePath);
        var resolved = NormalizeAbsolute(Path.Combine(normalizedBase, relativePath ?? string.Empty));

        if (!IsSameOrDescendant(resolved, normalizedBase))
        {
            throw new InvalidOperationException($"Path escapes its configured base: {relativePath}");
        }

        return resolved;
    }

    public static bool IsSameOrDescendant(string candidate, string parent)
    {
        var normalizedCandidate = NormalizeAbsolute(candidate);
        var normalizedParent = NormalizeAbsolute(parent);

        if (normalizedCandidate.Equals(normalizedParent, PathComparison))
        {
            return true;
        }

        var prefix = normalizedParent + Path.DirectorySeparatorChar;
        return normalizedCandidate.StartsWith(prefix, PathComparison);
    }

    public static bool Overlaps(string left, string right)
    {
        return IsSameOrDescendant(left, right) || IsSameOrDescendant(right, left);
    }

    public static string ToPortableRelativePath(string basePath, string path)
    {
        return Path.GetRelativePath(basePath, path).Replace('\\', '/');
    }

    public static string ToPlatformPath(string portablePath)
    {
        return portablePath.Replace('/', Path.DirectorySeparatorChar);
    }
}
