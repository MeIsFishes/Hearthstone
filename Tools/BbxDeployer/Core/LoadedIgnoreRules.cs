namespace BbxDeployer.Core;

public sealed class LoadedIgnoreRules
{
    public List<IgnoreRule> Rules { get; } = [];

    public List<RuleFileSnapshot> RuleFiles { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<string> Errors { get; } = [];
}
