namespace BbxDeployer.Core;

public sealed class AppSettings
{
    public ProjectContext? Source { get; set; }

    public List<ProjectContext> Targets { get; set; } = [];

    public List<SyncItem> SyncItems { get; set; } = [];
}
