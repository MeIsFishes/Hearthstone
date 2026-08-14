using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class SyncPlanner(
    IgnoreRuleLoader ignoreRuleLoader,
    PathInclusionEvaluator inclusionEvaluator,
    ProjectLocator projectLocator,
    ProjectValidator projectValidator,
    IUnityEditorLocator unityEditorLocator)
{
    public Task<SyncPreview> CreatePreviewAsync(
        ProjectContext source,
        IReadOnlyCollection<ProjectContext> targets,
        IReadOnlyCollection<SyncItem> items,
        IProgress<PreviewProgress>? progress,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => CreatePreview(source, targets, items, progress, cancellationToken),
            cancellationToken);
    }

    private SyncPreview CreatePreview(
        ProjectContext source,
        IReadOnlyCollection<ProjectContext> targets,
        IReadOnlyCollection<SyncItem> items,
        IProgress<PreviewProgress>? progress,
        CancellationToken cancellationToken)
    {
        var enabledItems = items
            .SelectMany(SyncItemPathExpander.Expand)
            .ToList();
        var preview = new SyncPreview
        {
            Source = source.Clone(),
            Items = enabledItems
        };

        ValidateSource(preview, source, enabledItems);
        if (enabledItems.Count == 0)
        {
            preview.Errors.Add("Select at least one sync item.");
        }

        if (targets.Count == 0)
        {
            preview.Errors.Add("Add at least one destination project.");
        }

        var targetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in targets)
        {
            var targetPreview = new TargetPreview { Target = target.Clone() };
            preview.Targets.Add(targetPreview);
            ValidateTargetBasics(targetPreview, targetKeys);
            PopulateUnityBootstrapFiles(source, targetPreview, cancellationToken);
            projectValidator.ValidateTarget(source, targetPreview, enabledItems);
        }

        ValidateTargetOverlaps(preview.Targets);

        var destinationKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var itemIndex = 0; itemIndex < enabledItems.Count; itemIndex++)
        {
            var item = enabledItems[itemIndex];
            cancellationToken.ThrowIfCancellationRequested();
            ReportCounting(
                progress,
                $"Scanning {item.DisplayName}...",
                itemIndex,
                enabledItems.Count,
                0,
                completed: false);

            string sourceBase;
            string whitelistRoot;
            try
            {
                sourceBase = PathService.ResolveBase(source, item.SourceBase);
                whitelistRoot = PathService.ResolveInside(sourceBase, item.SourceRelativePath);
            }
            catch (Exception exception)
            {
                preview.Errors.Add($"{item.DisplayName}: {exception.Message}");
                ReportCounting(
                    progress,
                    $"Skipped {item.DisplayName}.",
                    itemIndex,
                    enabledItems.Count,
                    0,
                    completed: true);
                continue;
            }

            var isDirectory = Directory.Exists(whitelistRoot);
            var isFile = File.Exists(whitelistRoot);
            if (!isDirectory && !isFile)
            {
                preview.Errors.Add($"{item.DisplayName}: source path does not exist: {whitelistRoot}");
                ReportCounting(
                    progress,
                    $"Missing source path for {item.DisplayName}.",
                    itemIndex,
                    enabledItems.Count,
                    0,
                    completed: true);
                continue;
            }

            var ruleRoot = isFile
                ? Path.GetDirectoryName(whitelistRoot)!
                : whitelistRoot;
            var ignoreSession = ignoreRuleLoader.BeginScan(
                source,
                item,
                ruleRoot,
                cancellationToken);
            var loadedRules = ignoreSession.Result;

            var enumerationErrors = new List<string>();
            var scannedFileCount = 0;
            if (isFile)
            {
                var info = new FileInfo(whitelistRoot);
                scannedFileCount = 1;
                if (IsIncluded(
                        source,
                        item,
                        whitelistRoot,
                        loadedRules.Rules))
                {
                    AddPreviewFile(
                        preview,
                        item,
                        whitelistRoot,
                        string.Empty,
                        info,
                        destinationKeys);
                }
                else
                {
                    preview.ExcludedFileCount++;
                    preview.ExcludedBytes += info.Length;
                }
            }

            var sourcePaths = isDirectory
                ? FileTreeEnumerator.EnumerateFiles(
                    whitelistRoot,
                    enumerationErrors,
                    cancellationToken,
                    directory => inclusionEvaluator.IsIncluded(
                        directory,
                        loadedRules.Rules),
                    ignoreSession.EnterDirectory)
                : Enumerable.Empty<string>();
            foreach (var sourcePath in sourcePaths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                scannedFileCount++;
                if (scannedFileCount % 25 == 0)
                {
                    ReportCounting(
                        progress,
                        $"Scanning {item.DisplayName}: "
                        + $"{scannedFileCount:N0} files checked...",
                        itemIndex,
                        enabledItems.Count,
                        scannedFileCount,
                        completed: false);
                }

                var info = new FileInfo(sourcePath);

                if (!IsIncluded(
                        source,
                        item,
                        sourcePath,
                        loadedRules.Rules))
                {
                    preview.ExcludedFileCount++;
                    preview.ExcludedBytes += info.Length;
                    continue;
                }

                var relativePath = PathService.ToPortableRelativePath(whitelistRoot, sourcePath);
                AddPreviewFile(preview, item, sourcePath, relativePath, info, destinationKeys);
            }

            ReportCounting(
                progress,
                $"Scanned {item.DisplayName}: "
                + $"{scannedFileCount:N0} "
                + $"file{(scannedFileCount == 1 ? string.Empty : "s")} checked.",
                itemIndex,
                enabledItems.Count,
                scannedFileCount,
                completed: true);
            preview.Errors.AddRange(
                loadedRules.Errors.Select(error => $"{item.DisplayName}: {error}"));
            preview.Warnings.AddRange(
                loadedRules.Warnings.Select(warning => $"{item.DisplayName}: {warning}"));
            AddRuleSnapshots(preview, loadedRules.RuleFiles);
            preview.Errors.AddRange(enumerationErrors.Select(error => $"{item.DisplayName}: {error}"));
        }

        var totalComparisonFiles = preview.Files.Count * preview.Targets.Count;
        var completedComparisonFiles = 0;
        ReportComparisonProgress(
            progress,
            completedComparisonFiles,
            totalComparisonFiles,
            "File count complete. Preparing project comparison...");
        foreach (var targetPreview in preview.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PopulateTargetPathStatistics(targetPreview, enabledItems);
            PopulateTargetStatistics(
                preview,
                targetPreview,
                () =>
                {
                    completedComparisonFiles++;
                    if (completedComparisonFiles % 25 == 0
                        || completedComparisonFiles == totalComparisonFiles)
                    {
                        ReportComparisonProgress(
                            progress,
                            completedComparisonFiles,
                            totalComparisonFiles,
                            $"Checking {targetPreview.Target.DisplayName}: "
                            + $"{completedComparisonFiles:N0} of "
                            + $"{totalComparisonFiles:N0} files...");
                    }
                });
            DetermineTargetStatus(targetPreview);
        }

        ConfigureUnityProjectCreation(
            preview,
            source,
            progress,
            cancellationToken);

        foreach (var targetPreview in preview.Targets)
        {
            projectValidator.ValidateCapacity(targetPreview);
        }

        ReportFixedProgress(
            progress,
            100,
            completedComparisonFiles,
            totalComparisonFiles,
            "Preview complete.");
        return preview;
    }

    private void ValidateSource(
        SyncPreview preview,
        ProjectContext source,
        IReadOnlyCollection<SyncItem> enabledItems)
    {
        if (!Directory.Exists(source.RepositoryRoot))
        {
            preview.Errors.Add($"Source repository does not exist: {source.RepositoryRoot}");
        }

        if (enabledItems.Any(item => item.SourceBase == PathBaseKind.UnityProjectRoot)
            && !projectLocator.IsUnityProject(source.UnityProjectRoot))
        {
            preview.Errors.Add($"Source game project is not a valid Unity project: {source.UnityProjectRoot}");
        }
    }

    private static void ValidateTargetBasics(
        TargetPreview targetPreview,
        ISet<string> targetKeys)
    {
        var target = targetPreview.Target;
        string repositoryRoot;
        string unityRoot;

        try
        {
            repositoryRoot = PathService.NormalizeAbsolute(target.RepositoryRoot);
            unityRoot = PathService.NormalizeAbsolute(target.UnityProjectRoot);
        }
        catch (Exception exception)
        {
            targetPreview.Errors.Add(exception.Message);
            return;
        }

        if (!targetKeys.Add(unityRoot))
        {
            targetPreview.Errors.Add($"Duplicate destination project: {unityRoot}");
        }

    }

    private static void ValidateTargetOverlaps(IReadOnlyList<TargetPreview> targets)
    {
        for (var leftIndex = 0; leftIndex < targets.Count; leftIndex++)
        {
            for (var rightIndex = leftIndex + 1; rightIndex < targets.Count; rightIndex++)
            {
                var left = targets[leftIndex];
                var right = targets[rightIndex];
                try
                {
                    if (!PathService.Overlaps(
                            left.Target.RepositoryRoot,
                            right.Target.RepositoryRoot))
                    {
                        continue;
                    }

                    left.Errors.Add(
                        $"Destination repository overlaps '{right.Target.DisplayName}'.");
                    right.Errors.Add(
                        $"Destination repository overlaps '{left.Target.DisplayName}'.");
                }
                catch (Exception exception)
                {
                    left.Errors.Add(exception.Message);
                    right.Errors.Add(exception.Message);
                }
            }
        }
    }

    private static void AddRuleSnapshots(
        SyncPreview preview,
        IEnumerable<RuleFileSnapshot> snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            if (preview.RuleFiles.Any(
                    existing => existing.Path.Equals(snapshot.Path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            preview.RuleFiles.Add(snapshot);
        }
    }

    private static void AddPreviewFile(
        SyncPreview preview,
        SyncItem item,
        string sourcePath,
        string relativePath,
        FileInfo info,
        ISet<string> destinationKeys,
        string? destinationRelativeOverride = null)
    {
        var destinationRelative = destinationRelativeOverride
            ?? CombinePortable(item.TargetRelativePath, relativePath);
        var key = $"{item.TargetBase}:{destinationRelative}";
        if (!destinationKeys.Add(key))
        {
            preview.Errors.Add($"Multiple sync items write the same destination path: {destinationRelative}");
            return;
        }

        preview.Files.Add(new PreviewFile
        {
            SyncItemId = item.Id,
            SyncItemName = item.DisplayName,
            SourcePath = info.FullName,
            TargetBase = item.TargetBase,
            TargetRelativePath = destinationRelative,
            Length = info.Length,
            LastWriteTimeUtc = info.LastWriteTimeUtc
        });
    }

    private bool IsIncluded(
        ProjectContext source,
        SyncItem item,
        string path,
        IReadOnlyList<IgnoreRule> rules)
    {
        var isDeployerProjectList = item.Id.Equals(
                "shared-tools",
                StringComparison.OrdinalIgnoreCase)
            && PathService.NormalizeAbsolute(path).Equals(
                PathService.ResolveInside(
                    source.RepositoryRoot,
                    Path.Combine(
                        "Tools",
                        "BbxDeployer",
                        "BbxDeployer.projects.json")),
                StringComparison.OrdinalIgnoreCase);
        return inclusionEvaluator.IsIncluded(
            path,
            rules,
            ignoreGitIgnoreRules: isDeployerProjectList);
    }

    private static void PopulateTargetStatistics(
        SyncPreview preview,
        TargetPreview targetPreview,
        Action fileChecked)
    {
        foreach (var file in preview.Files)
        {
            try
            {
                var targetBase = PathService.ResolveBase(targetPreview.Target, file.TargetBase);
                var destination = PathService.ResolveInside(
                    targetBase,
                    PathService.ToPlatformPath(file.TargetRelativePath));

                if (File.Exists(destination))
                {
                    targetPreview.OverwriteFileCount++;
                    var destinationInfo = new FileInfo(destination);
                    if (destinationInfo.LastWriteTimeUtc > file.LastWriteTimeUtc)
                    {
                        targetPreview.RiskFiles.Add(new TargetRiskFile
                        {
                            TargetRelativePath = file.TargetRelativePath,
                            SourceLastWriteTimeUtc = file.LastWriteTimeUtc,
                            TargetLastWriteTimeUtc = destinationInfo.LastWriteTimeUtc
                        });
                        targetPreview.Warnings.Add(
                            $"Destination file is newer than source: {file.TargetRelativePath}");
                    }
                    else if (destinationInfo.LastWriteTimeUtc < file.LastWriteTimeUtc
                             || destinationInfo.Length != file.Length)
                    {
                        targetPreview.FilesNeedingSyncCount++;
                    }
                }
                else
                {
                    targetPreview.NewFileCount++;
                    targetPreview.FilesNeedingSyncCount++;
                }

                targetPreview.TotalBytes += file.Length;
            }
            catch (Exception exception)
            {
                targetPreview.Errors.Add(exception.Message);
            }
            finally
            {
                fileChecked();
            }
        }
    }

    private static void PopulateTargetPathStatistics(
        TargetPreview targetPreview,
        IReadOnlyCollection<SyncItem> enabledItems)
    {
        foreach (var item in enabledItems)
        {
            try
            {
                var targetBase = PathService.ResolveBase(targetPreview.Target, item.TargetBase);
                var targetPath = PathService.ResolveInside(targetBase, item.TargetRelativePath);
                if (Directory.Exists(targetPath) || File.Exists(targetPath))
                {
                    targetPreview.ExistingPathCount++;
                }
                else
                {
                    targetPreview.MissingPathCount++;
                }
            }
            catch (Exception exception)
            {
                targetPreview.Errors.Add($"{item.DisplayName}: {exception.Message}");
            }
        }
    }

    private static void DetermineTargetStatus(TargetPreview targetPreview)
    {
        if (targetPreview.RiskFiles.Count > 0)
        {
            targetPreview.Status = TargetSyncStatus.Warning;
            return;
        }

        if (targetPreview.RequiresUnityBootstrap
            || targetPreview.RequiresUnityProjectCreation)
        {
            targetPreview.Status = TargetSyncStatus.NewProject;
            return;
        }

        if (targetPreview.MissingPathCount > targetPreview.ExistingPathCount)
        {
            targetPreview.Status = TargetSyncStatus.NewProject;
            return;
        }

        targetPreview.Status =
            targetPreview.MissingPathCount > 0 || targetPreview.FilesNeedingSyncCount > 0
                ? TargetSyncStatus.WaitForSync
                : TargetSyncStatus.Synchronized;
    }

    private void PopulateUnityBootstrapFiles(
        ProjectContext source,
        TargetPreview targetPreview,
        CancellationToken cancellationToken)
    {
        if (projectLocator.IsUnityProject(targetPreview.Target.UnityProjectRoot)
            || !projectLocator.CanBootstrapUnityProject(targetPreview.Target))
        {
            return;
        }

        targetPreview.RequiresUnityBootstrap = true;
        var sourceSettings = Path.Combine(source.UnityProjectRoot, "ProjectSettings");
        var errors = new List<string>();
        foreach (var sourcePath in FileTreeEnumerator.EnumerateFiles(
                     sourceSettings,
                     errors,
                     cancellationToken))
        {
            var relativePath = CombinePortable(
                "ProjectSettings",
                PathService.ToPortableRelativePath(sourceSettings, sourcePath));
            AddUnityBootstrapFile(targetPreview, sourcePath, relativePath);
        }

        targetPreview.Errors.AddRange(
            errors.Select(error => $"Unity project bootstrap: {error}"));
        AddUnityBootstrapFile(
            targetPreview,
            Path.Combine(source.UnityProjectRoot, "Packages", "manifest.json"),
            "Packages/manifest.json",
            required: true);
        AddUnityBootstrapFile(
            targetPreview,
            Path.Combine(source.UnityProjectRoot, "Packages", "packages-lock.json"),
            "Packages/packages-lock.json",
            required: false);
    }

    private void ConfigureUnityProjectCreation(
        SyncPreview preview,
        ProjectContext source,
        IProgress<PreviewProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (!preview.Targets.Any(target => target.Status == TargetSyncStatus.NewProject))
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        ReportFixedProgress(
            progress,
            97,
            preview.Files.Count * preview.Targets.Count,
            preview.Files.Count * preview.Targets.Count,
            "Checking installed Unity versions...");
        preview.UnityEditors.AddRange(unityEditorLocator.DiscoverInstalledEditors());
        if (preview.UnityEditors.Count == 0)
        {
            preview.Warnings.Add(
                "No installed Unity Editor was found. New projects will use the "
                + "create-only compatibility bootstrap.");
            return;
        }

        var sourceVersion = projectLocator.ReadUnityVersion(source.UnityProjectRoot);
        foreach (var targetPreview in preview.Targets.Where(target =>
                     target.RequiresUnityBootstrap
                     && projectLocator.CanCreateUnityProject(target.Target)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var editor = FindEditor(
                    preview.UnityEditors,
                    targetPreview.Target.UnityEditorVersion)
                ?? FindEditor(preview.UnityEditors, sourceVersion)
                ?? preview.UnityEditors[0];

            targetPreview.TotalBytes -= targetPreview.UnityBootstrapFiles.Sum(
                file => file.Length);
            targetPreview.UnityBootstrapFiles.Clear();
            targetPreview.RequiresUnityBootstrap = false;
            targetPreview.RequiresUnityProjectCreation = true;
            targetPreview.Target.UnityEditorVersion = editor.Version;
            targetPreview.UnityEditorExecutablePath = editor.ExecutablePath;
            AddUnityBootstrapFile(
                targetPreview,
                Path.Combine(source.UnityProjectRoot, "Packages", "manifest.json"),
                "Packages/manifest.json",
                required: true,
                overwrite: true);
            AddUnityBootstrapFile(
                targetPreview,
                Path.Combine(source.UnityProjectRoot, "Packages", "packages-lock.json"),
                "Packages/packages-lock.json",
                required: false,
                overwrite: true);
        }
    }

    private static UnityEditorInstallation? FindEditor(
        IEnumerable<UnityEditorInstallation> editors,
        string? version)
    {
        return string.IsNullOrWhiteSpace(version)
            ? null
            : editors.FirstOrDefault(editor => editor.Version.Equals(
                version,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void ReportCounting(
        IProgress<PreviewProgress>? progress,
        string message,
        int itemIndex,
        int itemCount,
        int scannedFileCount,
        bool completed)
    {
        var itemFraction = completed
            ? 1d
            : scannedFileCount == 0
                ? 0d
                : Math.Min(0.95, scannedFileCount / (scannedFileCount + 200d));
        var percentage = itemCount == 0
            ? 40
            : (itemIndex + itemFraction) * 40d / itemCount;
        progress?.Report(new PreviewProgress
        {
            Message = message,
            ExplicitPercentage = percentage
        });
    }

    private static void ReportComparisonProgress(
        IProgress<PreviewProgress>? progress,
        int completedFiles,
        int totalFiles,
        string message)
    {
        progress?.Report(new PreviewProgress
        {
            Message = message,
            CompletedFiles = completedFiles,
            TotalFiles = totalFiles,
            ExplicitPercentage = 40
                + (totalFiles == 0
                    ? 55
                    : completedFiles * 55d / totalFiles)
        });
    }

    private static void ReportFixedProgress(
        IProgress<PreviewProgress>? progress,
        double percentage,
        int completedFiles,
        int totalFiles,
        string message)
    {
        progress?.Report(new PreviewProgress
        {
            Message = message,
            CompletedFiles = completedFiles,
            TotalFiles = totalFiles,
            ExplicitPercentage = percentage
        });
    }

    private static void AddUnityBootstrapFile(
        TargetPreview targetPreview,
        string sourcePath,
        string targetRelativePath,
        bool required = true,
        bool overwrite = false)
    {
        if (!File.Exists(sourcePath))
        {
            if (required)
            {
                targetPreview.Errors.Add(
                    $"Unity project bootstrap source file is missing: {sourcePath}");
            }

            return;
        }

        var targetPath = PathService.ResolveInside(
            targetPreview.Target.UnityProjectRoot,
            PathService.ToPlatformPath(targetRelativePath));
        if (!overwrite && File.Exists(targetPath))
        {
            return;
        }

        var info = new FileInfo(sourcePath);
        targetPreview.UnityBootstrapFiles.Add(new UnityBootstrapFile
        {
            SourcePath = info.FullName,
            TargetRelativePath = targetRelativePath,
            Length = info.Length,
            LastWriteTimeUtc = info.LastWriteTimeUtc,
            Overwrite = overwrite
        });
        targetPreview.TotalBytes += info.Length;
    }

    private static string CombinePortable(string left, string right)
    {
        var normalizedLeft = left.Replace('\\', '/').Trim('/');
        var normalizedRight = right.Replace('\\', '/').Trim('/');
        return string.IsNullOrEmpty(normalizedRight)
            ? normalizedLeft
            : $"{normalizedLeft}/{normalizedRight}";
    }

}
