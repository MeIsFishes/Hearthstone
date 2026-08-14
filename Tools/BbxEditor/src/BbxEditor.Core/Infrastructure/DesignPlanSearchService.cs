namespace BbxEditor.Infrastructure;

public static class DesignPlanSearchService
{
    public const int DefaultMaxSemanticMatches = 5;

    public static string GetVectorName(IndexedDesignPlanDocument document)
    {
        var title = VectorSearchNameNormalizer.NormalizeQuery(document.Title);
        return string.IsNullOrWhiteSpace(title)
            ? VectorSearchNameNormalizer.NormalizeFileName(document.FileName)
            : title;
    }

    public static IReadOnlyList<IndexedDesignPlanDocument> FindLiteralMatches(
        IEnumerable<IndexedDesignPlanDocument> documents,
        string query)
    {
        var trimmedQuery = query.Trim();
        if (trimmedQuery.Length == 0) return documents.ToArray();
        return documents
            .Select(document => (Document: document, Rank: GetLiteralRank(document, trimmedQuery)))
            .Where(item => item.Rank is not null)
            .OrderBy(item => item.Rank)
            .ThenBy(item => item.Document.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Document.FileName, StringComparer.OrdinalIgnoreCase)
            .Select(item => item.Document)
            .ToArray();
    }

    public static int? GetLiteralRank(IndexedDesignPlanDocument document, string query)
    {
        var normalizedQuery = VectorSearchNameNormalizer.NormalizeQuery(query);
        var normalizedTitle = VectorSearchNameNormalizer.NormalizeQuery(document.Title);
        var normalizedFileName = VectorSearchNameNormalizer.NormalizeFileName(document.FileName);
        if (document.Title.Equals(query, StringComparison.CurrentCultureIgnoreCase) ||
            normalizedTitle.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)) return 0;
        if (document.FileName.Equals(query, StringComparison.OrdinalIgnoreCase) ||
            normalizedFileName.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)) return 1;
        if (document.Title.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
            normalizedTitle.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)) return 2;
        if (document.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            normalizedFileName.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)) return 3;
        return null;
    }

    public static IReadOnlyList<IndexedDesignPlanDocument> MergeVectorMatches(
        IEnumerable<IndexedDesignPlanDocument> documents,
        IReadOnlyCollection<IndexedDesignPlanDocument> literalMatches,
        IReadOnlyList<string> rankedVectorNames,
        int maxSemanticMatches = DefaultMaxSemanticMatches)
    {
        var literalDocuments = literalMatches.ToArray();
        if (maxSemanticMatches <= 0 || rankedVectorNames.Count == 0) return literalDocuments;

        var literalPaths = literalDocuments.Select(document => document.FullPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var ranks = rankedVectorNames.Select((name, rank) => (name, rank))
            .GroupBy(item => item.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Min(item => item.rank), StringComparer.OrdinalIgnoreCase);
        var semanticDocuments = documents
            .Where(document => !literalPaths.Contains(document.FullPath) &&
                               ranks.ContainsKey(GetVectorName(document)))
            .OrderBy(document => ranks[GetVectorName(document)])
            .ThenBy(document => document.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(document => document.FileName, StringComparer.OrdinalIgnoreCase)
            .Take(maxSemanticMatches);
        return literalDocuments.Concat(semanticDocuments).ToArray();
    }

    public static IReadOnlyList<string> BuildVectorCorpus(
        IEnumerable<IndexedProjectFile> projectFiles,
        IEnumerable<string> taskTypeNames,
        IEnumerable<IndexedDesignPlanDocument> designPlans) =>
        projectFiles.Select(file => VectorSearchNameNormalizer.NormalizeFileName(file.FileName))
            .Concat(taskTypeNames.Select(VectorSearchNameNormalizer.NormalizeTaskName))
            .Concat(designPlans.Select(GetVectorName))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
}
