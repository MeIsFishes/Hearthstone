using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class IgnoreRuleScanSession
{
    private readonly IgnoreRuleLoader _loader;

    internal IgnoreRuleScanSession(
        IgnoreRuleLoader loader,
        LoadedIgnoreRules result,
        HashSet<string> loadedFiles,
        int nextGitRuleOrder)
    {
        _loader = loader;
        Result = result;
        LoadedFiles = loadedFiles;
        NextGitRuleOrder = nextGitRuleOrder;
    }

    public LoadedIgnoreRules Result { get; }

    internal HashSet<string> LoadedFiles { get; }

    internal int NextGitRuleOrder { get; set; }

    public void EnterDirectory(string directory)
    {
        _loader.LoadDirectoryIgnoreFile(this, directory);
    }
}
