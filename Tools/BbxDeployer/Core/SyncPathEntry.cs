namespace BbxDeployer.Core;

public sealed class SyncPathEntry
{
    public string RelativePath { get; set; } = string.Empty;

    public List<string> ManualExcludePatterns { get; set; } = [];

    public SyncPathEntry Clone()
    {
        return new SyncPathEntry
        {
            RelativePath = RelativePath,
            ManualExcludePatterns = [.. ManualExcludePatterns]
        };
    }
}
