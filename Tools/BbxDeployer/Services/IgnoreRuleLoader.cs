using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class IgnoreRuleLoader
{
    private const int AdditionalRuleOrderStart = 1_000_000_000;

    public LoadedIgnoreRules Load(
        ProjectContext source,
        SyncItem item,
        string whitelistRoot,
        CancellationToken cancellationToken)
    {
        var session = BeginScan(source, item, whitelistRoot, cancellationToken);
        DiscoverNestedIgnoreFiles(session, whitelistRoot, cancellationToken);
        return session.Result;
    }

    public IgnoreRuleScanSession BeginScan(
        ProjectContext source,
        SyncItem item,
        string whitelistRoot,
        CancellationToken cancellationToken)
    {
        var result = new LoadedIgnoreRules();
        var manualOrder = 0;
        foreach (var manualPattern in item.ManualExcludePatterns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddRule(
                result,
                manualPattern,
                IgnoreRuleKind.Manual,
                whitelistRoot,
                "User",
                manualOrder++);
        }

        var loadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var gitRuleOrder = manualOrder;
        if (item.UseGitIgnoreFiles)
        {
            LoadAncestorIgnoreFiles(
                result,
                loadedFiles,
                source.RepositoryRoot,
                whitelistRoot,
                ref gitRuleOrder,
                cancellationToken);
        }

        var session = new IgnoreRuleScanSession(
            this,
            result,
            loadedFiles,
            gitRuleOrder);
        var additionalOrder = AdditionalRuleOrderStart;
        foreach (var reference in item.AdditionalIgnoreFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filePath = PathService.NormalizeAbsolute(reference.FilePath);
            var baseDirectory = string.IsNullOrWhiteSpace(reference.BaseDirectory)
                ? Path.GetDirectoryName(filePath)!
                : PathService.NormalizeAbsolute(reference.BaseDirectory);
            LoadFile(result, filePath, baseDirectory, ref additionalOrder);
        }

        return session;
    }

    internal void LoadDirectoryIgnoreFile(
        IgnoreRuleScanSession session,
        string directory)
    {
        var order = session.NextGitRuleOrder;
        LoadIfExists(
            session.Result,
            session.LoadedFiles,
            Path.Combine(directory, ".gitignore"),
            ref order);
        session.NextGitRuleOrder = order;
    }

    private static void LoadAncestorIgnoreFiles(
        LoadedIgnoreRules result,
        ISet<string> loadedFiles,
        string repositoryRoot,
        string whitelistRoot,
        ref int order,
        CancellationToken cancellationToken)
    {
        var normalizedRepository = PathService.NormalizeAbsolute(repositoryRoot);
        var normalizedWhitelist = PathService.NormalizeAbsolute(whitelistRoot);
        if (!PathService.IsSameOrDescendant(normalizedWhitelist, normalizedRepository))
        {
            return;
        }

        var relative = Path.GetRelativePath(normalizedRepository, normalizedWhitelist);
        var current = normalizedRepository;
        LoadIfExists(
            result,
            loadedFiles,
            Path.Combine(current, ".gitignore"),
            ref order);

        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            current = Path.Combine(current, segment);
            LoadIfExists(
                result,
                loadedFiles,
                Path.Combine(current, ".gitignore"),
                ref order);
        }
    }

    private static void DiscoverNestedIgnoreFiles(
        IgnoreRuleScanSession session,
        string whitelistRoot,
        CancellationToken cancellationToken)
    {
        var evaluator = new PathInclusionEvaluator();
        var pending = new Stack<string>();
        pending.Push(PathService.NormalizeAbsolute(whitelistRoot));
        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            session.EnterDirectory(directory);

            IReadOnlyList<DirectoryInfo> children;
            try
            {
                children = new DirectoryInfo(directory).EnumerateDirectories().ToList();
            }
            catch (Exception exception) when (
                exception is IOException or UnauthorizedAccessException)
            {
                session.Result.Errors.Add(
                    $"Cannot enumerate '{directory}': {exception.Message}");
                continue;
            }

            foreach (var child in children)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if ((child.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        session.Result.Errors.Add(
                            $"Reparse points are not supported: {child.FullName}");
                        continue;
                    }
                }
                catch (Exception exception) when (
                    exception is IOException or UnauthorizedAccessException)
                {
                    session.Result.Errors.Add(
                        $"Cannot inspect '{child.FullName}': {exception.Message}");
                    continue;
                }

                if (evaluator.IsIncluded(child.FullName, session.Result.Rules))
                {
                    pending.Push(child.FullName);
                }
            }
        }
    }

    private static void LoadIfExists(
        LoadedIgnoreRules result,
        ISet<string> loadedFiles,
        string path,
        ref int order)
    {
        if (!File.Exists(path))
        {
            return;
        }

        var normalizedPath = PathService.NormalizeAbsolute(path);
        if (loadedFiles.Add(normalizedPath))
        {
            LoadFile(
                result,
                normalizedPath,
                Path.GetDirectoryName(normalizedPath)!,
                ref order);
        }
    }

    private static void LoadFile(
        LoadedIgnoreRules result,
        string filePath,
        string baseDirectory,
        ref int order)
    {
        if (!File.Exists(filePath))
        {
            result.Errors.Add($"Ignore file does not exist: {filePath}");
            return;
        }

        try
        {
            var info = new FileInfo(filePath);
            result.RuleFiles.Add(new RuleFileSnapshot
            {
                Path = info.FullName,
                Length = info.Length,
                LastWriteTimeUtc = info.LastWriteTimeUtc
            });

            foreach (var line in File.ReadLines(filePath))
            {
                AddRule(
                    result,
                    line,
                    IgnoreRuleKind.GitIgnore,
                    baseDirectory,
                    filePath,
                    order++);
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException)
        {
            result.Errors.Add(
                $"Cannot read ignore file '{filePath}': {exception.Message}");
        }
    }

    private static void AddRule(
        LoadedIgnoreRules result,
        string line,
        IgnoreRuleKind kind,
        string baseDirectory,
        string source,
        int order)
    {
        if (!GitIgnorePattern.TryParse(line, out var parsed, out var error))
        {
            if (error is not null)
            {
                result.Errors.Add($"{source}: {error}");
            }

            return;
        }

        result.Rules.Add(new IgnoreRule
        {
            Kind = kind,
            Pattern = parsed!.Original,
            BaseDirectory = PathService.NormalizeAbsolute(baseDirectory),
            Source = source,
            IsNegation = parsed.IsNegation,
            DirectoryOnly = parsed.DirectoryOnly,
            Order = order
        });
    }
}
