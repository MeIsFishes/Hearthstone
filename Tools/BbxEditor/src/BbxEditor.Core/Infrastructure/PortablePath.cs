namespace BbxEditor.Infrastructure;

public static class PortablePath
{
    public static string Resolve(string baseDirectory, string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath)) return string.Empty;
        return Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(baseDirectory, configuredPath));
    }

    public static string MakeRelative(string baseDirectory, string path) =>
        Path.GetRelativePath(Path.GetFullPath(baseDirectory), Path.GetFullPath(path)).Replace('\\', '/');
}
