namespace BbxDeployer.Services;

public static class FileTreeEnumerator
{
    public static IEnumerable<string> EnumerateFiles(
        string root,
        ICollection<string> errors,
        CancellationToken cancellationToken,
        Func<string, bool>? shouldTraverseDirectory = null,
        Action<string>? onDirectoryEntered = null)
    {
        var pending = new Stack<string>();
        pending.Push(PathService.NormalizeAbsolute(root));

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            onDirectoryEntered?.Invoke(directory);
            IReadOnlyList<FileSystemInfo> entries;

            try
            {
                entries = new DirectoryInfo(directory)
                    .EnumerateFileSystemInfos()
                    .ToList();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                errors.Add($"Cannot enumerate '{directory}': {exception.Message}");
                continue;
            }

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                FileAttributes attributes;
                try
                {
                    attributes = entry.Attributes;
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    errors.Add($"Cannot inspect '{entry.FullName}': {exception.Message}");
                    continue;
                }

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    errors.Add($"Reparse points are not supported: {entry.FullName}");
                    continue;
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (shouldTraverseDirectory?.Invoke(entry.FullName) ?? true)
                    {
                        pending.Push(entry.FullName);
                    }
                }
                else
                {
                    yield return entry.FullName;
                }
            }
        }
    }
}
