namespace BbxDeployer.Core;

public sealed class SyncItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string DisplayName { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public bool IsBuiltIn { get; set; }

    public PathBaseKind SourceBase { get; set; }

    public string SourceRelativePath { get; set; } = string.Empty;

    public PathBaseKind TargetBase { get; set; }

    public string TargetRelativePath { get; set; } = string.Empty;

    public bool IncludeCompanionMeta { get; set; }

    public bool UseGitIgnoreFiles { get; set; }

    public List<SyncPathEntry> WhitelistPaths { get; set; } = [];

    public List<string> ManualExcludePatterns { get; set; } = [];

    public List<IgnoreFileReference> AdditionalIgnoreFiles { get; set; } = [];

    public SyncItem Clone()
    {
        return new SyncItem
        {
            Id = Id,
            DisplayName = DisplayName,
            Enabled = Enabled,
            IsBuiltIn = IsBuiltIn,
            SourceBase = SourceBase,
            SourceRelativePath = SourceRelativePath,
            TargetBase = TargetBase,
            TargetRelativePath = TargetRelativePath,
            IncludeCompanionMeta = IncludeCompanionMeta,
            UseGitIgnoreFiles = UseGitIgnoreFiles,
            WhitelistPaths = WhitelistPaths.Select(path => path.Clone()).ToList(),
            ManualExcludePatterns = [.. ManualExcludePatterns],
            AdditionalIgnoreFiles = AdditionalIgnoreFiles
                .Select(reference => new IgnoreFileReference
                {
                    FilePath = reference.FilePath,
                    BaseDirectory = reference.BaseDirectory
                })
                .ToList()
        };
    }
}
