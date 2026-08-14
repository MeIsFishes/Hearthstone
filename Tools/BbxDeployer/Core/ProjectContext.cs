namespace BbxDeployer.Core;

public sealed class ProjectContext
{
    public string DisplayName { get; set; } = string.Empty;

    public string RepositoryRoot { get; set; } = string.Empty;

    public string UnityProjectRoot { get; set; } = string.Empty;

    public string UnityEditorVersion { get; set; } = string.Empty;

    public ProjectContext Clone()
    {
        return new ProjectContext
        {
            DisplayName = DisplayName,
            RepositoryRoot = RepositoryRoot,
            UnityProjectRoot = UnityProjectRoot,
            UnityEditorVersion = UnityEditorVersion
        };
    }
}
