namespace BbxDeployer.Core;

public sealed class SyncPreview
{
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;

    public required ProjectContext Source { get; init; }

    public required IReadOnlyList<SyncItem> Items { get; init; }

    public List<PreviewFile> Files { get; } = [];

    public List<TargetPreview> Targets { get; } = [];

    public List<UnityEditorInstallation> UnityEditors { get; } = [];

    public List<RuleFileSnapshot> RuleFiles { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<string> Errors { get; } = [];

    public int ExcludedFileCount { get; set; }

    public long ExcludedBytes { get; set; }

    public bool HasBlockingErrors => Errors.Count > 0 || Targets.Any(target => target.Errors.Count > 0);

    public long IncludedBytes => Files.Sum(file => file.Length);
}
