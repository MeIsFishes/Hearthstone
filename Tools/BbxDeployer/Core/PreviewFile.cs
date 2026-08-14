namespace BbxDeployer.Core;

public sealed class PreviewFile
{
    public required string SyncItemId { get; init; }

    public required string SyncItemName { get; init; }

    public required string SourcePath { get; init; }

    public required PathBaseKind TargetBase { get; init; }

    public required string TargetRelativePath { get; init; }

    public long Length { get; init; }

    public DateTime LastWriteTimeUtc { get; init; }
}
