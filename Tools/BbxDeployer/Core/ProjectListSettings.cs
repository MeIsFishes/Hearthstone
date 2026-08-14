namespace BbxDeployer.Core;

public sealed class ProjectListSettings
{
    public ProjectContext? Source { get; set; }

    public List<ProjectContext> Targets { get; set; } = [];
}
