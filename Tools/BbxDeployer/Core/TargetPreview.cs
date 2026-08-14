namespace BbxDeployer.Core;

public sealed class TargetPreview
{
    public required ProjectContext Target { get; init; }

    public TargetSyncStatus Status { get; set; }

    public bool RequiresUnityBootstrap { get; set; }

    public bool RequiresUnityProjectCreation { get; set; }

    public string UnityEditorExecutablePath { get; set; } = string.Empty;

    public int ExistingPathCount { get; set; }

    public int MissingPathCount { get; set; }

    public int NewFileCount { get; set; }

    public int OverwriteFileCount { get; set; }

    public int FilesNeedingSyncCount { get; set; }

    public long TotalBytes { get; set; }

    public List<TargetRiskFile> RiskFiles { get; } = [];

    public List<UnityBootstrapFile> UnityBootstrapFiles { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<string> Errors { get; } = [];
}
