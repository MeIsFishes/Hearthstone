namespace BbxDeployer.Core;

public sealed class TargetRiskFile
{
    public required string TargetRelativePath { get; init; }

    public DateTime SourceLastWriteTimeUtc { get; init; }

    public DateTime TargetLastWriteTimeUtc { get; init; }
}
