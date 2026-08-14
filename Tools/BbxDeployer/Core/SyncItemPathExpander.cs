namespace BbxDeployer.Core;

public static class SyncItemPathExpander
{
    public static IReadOnlyList<SyncPathEntry> GetConfiguredPaths(SyncItem item)
    {
        if (item.WhitelistPaths.Count > 0)
        {
            return item.WhitelistPaths;
        }

        var legacyPath = SyncPathTemplate.ToProjectRelativePath(item);
        if (string.IsNullOrWhiteSpace(legacyPath))
        {
            return [];
        }

        return
        [
            new SyncPathEntry
            {
                RelativePath = legacyPath,
                ManualExcludePatterns = [.. item.ManualExcludePatterns]
            }
        ];
    }

    public static IReadOnlyList<SyncItem> Expand(SyncItem item)
    {
        var result = new List<SyncItem>();
        foreach (var path in GetConfiguredPaths(item))
        {
            var clone = item.Clone();
            clone.Enabled = true;
            SyncPathTemplate.ApplyProjectRelativePath(clone, path.RelativePath);
            clone.IncludeCompanionMeta = false;
            clone.UseGitIgnoreFiles = true;
            clone.ManualExcludePatterns = [.. path.ManualExcludePatterns];
            clone.AdditionalIgnoreFiles = [];
            clone.WhitelistPaths = [];
            result.Add(clone);
        }

        return result;
    }
}
