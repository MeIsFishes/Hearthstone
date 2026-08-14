namespace BbxDeployer.Core;

public sealed class UnityBootstrapFile
{
    public required string SourcePath { get; init; }

    public required string TargetRelativePath { get; init; }

    public long Length { get; init; }

    public DateTime LastWriteTimeUtc { get; init; }

    public bool Overwrite { get; init; }
}
