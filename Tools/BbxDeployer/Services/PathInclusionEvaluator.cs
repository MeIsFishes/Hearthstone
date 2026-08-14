using BbxDeployer.Core;

namespace BbxDeployer.Services;

public sealed class PathInclusionEvaluator
{
    private readonly object _cacheLock = new();
    private IReadOnlyList<IgnoreRule>? _cachedRules;
    private int _cachedRuleCount;
    private CompiledRule[] _compiledRules = [];

    public bool IsIncluded(
        string path,
        IReadOnlyList<IgnoreRule> rules,
        bool ignoreGitIgnoreRules = false)
    {
        var normalizedPath = PathService.NormalizeAbsolute(path);
        var compiledRules = GetCompiledRules(rules);
        var manualExcluded = false;
        var gitIncluded = true;

        ApplyRules(
            normalizedPath,
            compiledRules,
            ignoreGitIgnoreRules,
            ref manualExcluded,
            ref gitIncluded);

        if (normalizedPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
        {
            ApplyRules(
                normalizedPath[..^5],
                compiledRules,
                ignoreGitIgnoreRules,
                ref manualExcluded,
                ref gitIncluded);
        }

        return !manualExcluded && gitIncluded;
    }

    private static void ApplyRules(
        string normalizedPath,
        IReadOnlyList<CompiledRule> rules,
        bool ignoreGitIgnoreRules,
        ref bool manualExcluded,
        ref bool gitIncluded)
    {
        foreach (var rule in rules)
        {
            if (ignoreGitIgnoreRules
                && rule.Rule.Kind == IgnoreRuleKind.GitIgnore)
            {
                continue;
            }

            if (!TryGetRuleRelativePath(
                    normalizedPath,
                    rule.BaseDirectory,
                    out var relativePath)
                || !rule.Pattern.IsMatch(relativePath))
            {
                continue;
            }

            if (rule.Rule.Kind == IgnoreRuleKind.Manual)
            {
                manualExcluded = true;
            }
            else
            {
                gitIncluded = rule.Rule.IsNegation;
            }
        }
    }

    private static bool TryGetRuleRelativePath(
        string normalizedPath,
        string baseDirectory,
        out string relativePath)
    {
        if (!PathService.IsSameOrDescendant(normalizedPath, baseDirectory))
        {
            relativePath = string.Empty;
            return false;
        }

        relativePath = PathService.ToPortableRelativePath(baseDirectory, normalizedPath);
        return true;
    }

    private CompiledRule[] GetCompiledRules(IReadOnlyList<IgnoreRule> rules)
    {
        lock (_cacheLock)
        {
            if (ReferenceEquals(_cachedRules, rules) && _cachedRuleCount == rules.Count)
            {
                return _compiledRules;
            }

            _compiledRules = rules
                .OrderBy(rule => rule.Order)
                .Select(rule =>
                {
                    GitIgnorePattern.TryParse(rule.Pattern, out var pattern, out _);
                    return pattern is null
                        ? null
                        : new CompiledRule(
                            rule,
                            PathService.NormalizeAbsolute(rule.BaseDirectory),
                            pattern);
                })
                .Where(rule => rule is not null)
                .Cast<CompiledRule>()
                .ToArray();
            _cachedRules = rules;
            _cachedRuleCount = rules.Count;
            return _compiledRules;
        }
    }

    private sealed record CompiledRule(
        IgnoreRule Rule,
        string BaseDirectory,
        GitIgnorePattern Pattern);
}
