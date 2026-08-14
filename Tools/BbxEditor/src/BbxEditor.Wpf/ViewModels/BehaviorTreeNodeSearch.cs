using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Wpf.ViewModels;

internal enum BehaviorTreeNodeSearchTier
{
    LiteralTitle,
    LiteralTypeName,
    VectorTitle,
    VectorTypeName,
}

internal sealed record BehaviorTreeNodeSearchResult(
    BehaviorNode Node,
    BehaviorTreeNodeSearchTier Tier,
    int Rank,
    int NodeOrder);

internal static class BehaviorTreeNodeSearch
{
    public static IReadOnlyList<BehaviorTreeNodeSearchResult> Rank(
        IReadOnlyList<BehaviorNode> nodes,
        string query,
        IReadOnlyList<string> rankedVectorTexts)
    {
        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length == 0) return [];
        var vectorRanks = rankedVectorTexts
            .Select((text, rank) => (text, rank))
            .GroupBy(item => item.text, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Min(item => item.rank), StringComparer.OrdinalIgnoreCase);
        var results = new List<BehaviorTreeNodeSearchResult>();
        for (var nodeOrder = 0; nodeOrder < nodes.Count; nodeOrder++)
        {
            var node = nodes[nodeOrder];
            if (node.Name.Contains(trimmedQuery, StringComparison.CurrentCultureIgnoreCase))
            {
                results.Add(new BehaviorTreeNodeSearchResult(node, BehaviorTreeNodeSearchTier.LiteralTitle, 0, nodeOrder));
                continue;
            }
            if (node.Task.TaskType.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(new BehaviorTreeNodeSearchResult(node, BehaviorTreeNodeSearchTier.LiteralTypeName, 0, nodeOrder));
                continue;
            }

            var titleText = GetTitleVectorText(node);
            if (vectorRanks.TryGetValue(titleText, out var titleRank))
            {
                results.Add(new BehaviorTreeNodeSearchResult(node, BehaviorTreeNodeSearchTier.VectorTitle, titleRank, nodeOrder));
                continue;
            }
            var typeText = GetTypeVectorText(node);
            if (vectorRanks.TryGetValue(typeText, out var typeRank))
                results.Add(new BehaviorTreeNodeSearchResult(node, BehaviorTreeNodeSearchTier.VectorTypeName, typeRank, nodeOrder));
        }

        return results
            .OrderBy(result => result.Tier)
            .ThenBy(result => result.Rank)
            .ThenBy(result => result.NodeOrder)
            .ToArray();
    }

    public static string GetTitleVectorText(BehaviorNode node) =>
        VectorSearchNameNormalizer.NormalizeQuery(node.Name);

    public static string GetTypeVectorText(BehaviorNode node) =>
        VectorSearchNameNormalizer.NormalizeTaskName(node.Task.TaskType);
}
