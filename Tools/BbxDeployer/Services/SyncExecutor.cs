using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class SyncExecutor
{
    private const double ValidationProgressEnd = 10d;

    private readonly IgnoreRuleLoader _ignoreRuleLoader;
    private readonly IUnityProjectCreator _unityProjectCreator;

    public SyncExecutor(IgnoreRuleLoader ignoreRuleLoader)
        : this(
            ignoreRuleLoader,
            new UnityProjectCreator(new ProjectLocator()))
    {
    }

    public SyncExecutor(
        IgnoreRuleLoader ignoreRuleLoader,
        IUnityProjectCreator unityProjectCreator)
    {
        _ignoreRuleLoader = ignoreRuleLoader;
        _unityProjectCreator = unityProjectCreator;
    }

    public async Task<SyncResult> ExecuteAsync(
        SyncPreview preview,
        IProgress<SyncProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (preview.HasBlockingErrors)
        {
            throw new InvalidOperationException("The preview contains blocking errors.");
        }

        await Task.Run(
            () => ValidatePreviewSnapshot(preview, progress, cancellationToken),
            cancellationToken);

        var logger = new RunLogger();
        var result = new SyncResult();
        var totalFiles = preview.Files.Count * preview.Targets.Count
            + preview.Targets.Sum(target => target.UnityBootstrapFiles.Count);
        var totalBytes = preview.IncludedBytes * preview.Targets.Count
            + preview.Targets.Sum(target => target.UnityBootstrapFiles.Sum(file => file.Length));
        var completedFiles = 0;
        long completedBytes = 0;

        logger.Write($"Sync started. Targets={preview.Targets.Count}, Files={totalFiles}, Bytes={totalBytes}.");

        foreach (var targetPreview in preview.Targets)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                result.Cancelled = true;
                break;
            }

            var targetResult = new TargetSyncResult
            {
                TargetName = targetPreview.Target.DisplayName
            };
            result.Targets.Add(targetResult);
            logger.Write($"Target started: {targetResult.TargetName}");

            try
            {
                if (targetPreview.RequiresUnityProjectCreation)
                {
                    progress?.Report(new SyncProgress
                    {
                        TargetName = targetPreview.Target.DisplayName,
                        SyncItemName = "Unity Project Creation",
                        CurrentFile =
                            $"Starting Unity {targetPreview.Target.UnityEditorVersion}...",
                        IsIndeterminate = true,
                        PercentageOverride = ValidationProgressEnd,
                        CompletedFiles = completedFiles,
                        TotalFiles = totalFiles,
                        CompletedBytes = completedBytes,
                        TotalBytes = totalBytes
                    });
                    await _unityProjectCreator.CreateAsync(
                        new UnityEditorInstallation
                        {
                            Version = targetPreview.Target.UnityEditorVersion,
                            ExecutablePath = targetPreview.UnityEditorExecutablePath
                        },
                        targetPreview.Target.UnityProjectRoot,
                        cancellationToken);
                    logger.Write(
                        $"Unity project created: {targetResult.TargetName}, "
                        + $"Version={targetPreview.Target.UnityEditorVersion}.");
                }

                if (targetPreview.RequiresUnityBootstrap)
                {
                    Directory.CreateDirectory(targetPreview.Target.UnityProjectRoot);
                    Directory.CreateDirectory(Path.Combine(
                        targetPreview.Target.UnityProjectRoot,
                        "Assets"));
                    Directory.CreateDirectory(Path.Combine(
                        targetPreview.Target.UnityProjectRoot,
                        "Packages"));
                    Directory.CreateDirectory(Path.Combine(
                        targetPreview.Target.UnityProjectRoot,
                        "ProjectSettings"));
                }

                foreach (var file in targetPreview.UnityBootstrapFiles)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var destinationPath = PathService.ResolveInside(
                        targetPreview.Target.UnityProjectRoot,
                        PathService.ToPlatformPath(file.TargetRelativePath));
                    var copied = await CopyFileAtomicallyAsync(
                        file.SourcePath,
                        destinationPath,
                        overwrite: file.Overwrite,
                        cancellationToken);

                    if (copied)
                    {
                        targetResult.CopiedFiles++;
                        targetResult.CopiedBytes += file.Length;
                    }

                    completedFiles++;
                    completedBytes += file.Length;
                    progress?.Report(new SyncProgress
                    {
                        TargetName = targetPreview.Target.DisplayName,
                        SyncItemName = "Unity Project Bootstrap",
                        CurrentFile = file.TargetRelativePath,
                        PercentageOverride = CalculateExecutionPercentage(
                            completedFiles,
                            totalFiles),
                        CompletedFiles = completedFiles,
                        TotalFiles = totalFiles,
                        CompletedBytes = completedBytes,
                        TotalBytes = totalBytes
                    });
                }

                foreach (var file in preview.Files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var targetBase = PathService.ResolveBase(targetPreview.Target, file.TargetBase);
                    var destinationPath = PathService.ResolveInside(
                        targetBase,
                        PathService.ToPlatformPath(file.TargetRelativePath));

                    await CopyFileAtomicallyAsync(
                        file.SourcePath,
                        destinationPath,
                        overwrite: true,
                        cancellationToken);

                    targetResult.CopiedFiles++;
                    targetResult.CopiedBytes += file.Length;
                    completedFiles++;
                    completedBytes += file.Length;

                    progress?.Report(new SyncProgress
                    {
                        TargetName = targetPreview.Target.DisplayName,
                        SyncItemName = file.SyncItemName,
                        CurrentFile = file.TargetRelativePath,
                        PercentageOverride = CalculateExecutionPercentage(
                            completedFiles,
                            totalFiles),
                        CompletedFiles = completedFiles,
                        TotalFiles = totalFiles,
                        CompletedBytes = completedBytes,
                        TotalBytes = totalBytes
                    });
                }

                targetResult.Succeeded = true;
                logger.Write(
                    $"Target completed: {targetResult.TargetName}, "
                    + $"Files={targetResult.CopiedFiles}, Bytes={targetResult.CopiedBytes}.");
            }
            catch (OperationCanceledException)
            {
                targetResult.Cancelled = true;
                result.Cancelled = true;
                logger.Write($"Target cancelled: {targetResult.TargetName}");
                break;
            }
            catch (Exception exception)
            {
                targetResult.Error = exception.Message;
                logger.Write($"Target failed: {targetResult.TargetName}. {exception}");
            }
        }

        result.FinishedAt = DateTimeOffset.Now;
        if (!result.Cancelled && result.Targets.All(target => target.Succeeded))
        {
            progress?.Report(new SyncProgress
            {
                SyncItemName = "Sync Complete",
                CurrentFile = "Finalizing log...",
                PercentageOverride = 100,
                CompletedFiles = totalFiles,
                TotalFiles = totalFiles,
                CompletedBytes = totalBytes,
                TotalBytes = totalBytes
            });
        }
        logger.Write(result.Cancelled ? "Sync cancelled with partial changes." : "Sync finished.");
        result.LogPath = await logger.SaveAsync(CancellationToken.None);
        return result;
    }

    private void ValidatePreviewSnapshot(
        SyncPreview preview,
        IProgress<SyncProgress>? progress,
        CancellationToken cancellationToken)
    {
        var bootstrapFiles = preview.Targets
            .SelectMany(target => target.UnityBootstrapFiles)
            .ToList();
        var totalSteps = preview.Files.Count + bootstrapFiles.Count + preview.RuleFiles.Count;
        var completedSteps = 0;
        ReportValidationProgress(
            progress,
            completedSteps,
            totalSteps,
            "Validating preview snapshot...");

        foreach (var file in preview.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file.SourcePath);
            if (!info.Exists
                || info.Length != file.Length
                || info.LastWriteTimeUtc != file.LastWriteTimeUtc)
            {
                throw new InvalidOperationException(
                    $"Source changed after preview. Run Preview again: {file.SourcePath}");
            }

            completedSteps++;
            ReportValidationProgress(
                progress,
                completedSteps,
                totalSteps,
                $"Validating source files: {completedSteps:N0} / {totalSteps:N0}");
        }

        foreach (var file in bootstrapFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file.SourcePath);
            if (!info.Exists
                || info.Length != file.Length
                || info.LastWriteTimeUtc != file.LastWriteTimeUtc)
            {
                throw new InvalidOperationException(
                    $"Unity bootstrap source changed after preview. Run Preview again: "
                    + file.SourcePath);
            }

            completedSteps++;
            ReportValidationProgress(
                progress,
                completedSteps,
                totalSteps,
                $"Validating bootstrap files: {completedSteps:N0} / {totalSteps:N0}");
        }

        foreach (var snapshot in preview.RuleFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(snapshot.Path);
            if (!info.Exists
                || info.Length != snapshot.Length
                || info.LastWriteTimeUtc != snapshot.LastWriteTimeUtc)
            {
                throw new InvalidOperationException(
                    $"Ignore rules changed after preview. Run Preview again: {snapshot.Path}");
            }

            completedSteps++;
            ReportValidationProgress(
                progress,
                completedSteps,
                totalSteps,
                $"Validating ignore rules: {completedSteps:N0} / {totalSteps:N0}");
        }
    }

    private static void ReportValidationProgress(
        IProgress<SyncProgress>? progress,
        int completedSteps,
        int totalSteps,
        string message)
    {
        var percentage = totalSteps == 0
            ? ValidationProgressEnd
            : completedSteps * ValidationProgressEnd / totalSteps;
        progress?.Report(new SyncProgress
        {
            SyncItemName = "Preview Validation",
            CurrentFile = message,
            PercentageOverride = percentage,
            CompletedFiles = completedSteps,
            TotalFiles = totalSteps
        });
    }

    private static double CalculateExecutionPercentage(
        int completedFiles,
        int totalFiles)
    {
        return totalFiles == 0
            ? 100
            : ValidationProgressEnd
                + completedFiles * (100d - ValidationProgressEnd) / totalFiles;
    }

    private static async Task<bool> CopyFileAtomicallyAsync(
        string sourcePath,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken)
    {
        if (!overwrite && File.Exists(destinationPath))
        {
            return false;
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException($"Invalid destination path: {destinationPath}");
        Directory.CreateDirectory(destinationDirectory);
        var temporaryPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.bbxdeploy.tmp");

        try
        {
            await using (var source = new FileStream(
                             sourcePath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             1024 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var destination = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             1024 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(destination, 1024 * 1024, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }

            File.SetLastWriteTimeUtc(temporaryPath, File.GetLastWriteTimeUtc(sourcePath));
            try
            {
                File.Move(temporaryPath, destinationPath, overwrite);
            }
            catch (IOException) when (!overwrite && File.Exists(destinationPath))
            {
                return false;
            }

            return true;
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
