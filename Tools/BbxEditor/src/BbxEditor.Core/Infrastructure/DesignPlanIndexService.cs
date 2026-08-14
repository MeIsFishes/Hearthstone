using System.Globalization;
using System.Text.RegularExpressions;

namespace BbxEditor.Infrastructure;

public sealed record IndexedDesignPlanDocument(
    string FullPath,
    string RelativePath,
    string Date,
    string FileName,
    string Title,
    string? State,
    string? Priority);

public sealed record IndexedDesignPlanDate(
    string Date,
    IReadOnlyList<IndexedDesignPlanDocument> Documents);

public sealed record DesignPlanDocumentContent(
    string Title,
    string? State,
    string? Priority,
    string? PlanPath,
    string? ReviewPath,
    string MarkdownBody);

public static partial class DesignPlanIndexService
{
    public static string ResolveDirectory(string gameProjectPath) =>
        Path.Combine(Path.GetFullPath(gameProjectPath), "AutoDoc", "DesignPlan");

    public static IReadOnlyList<IndexedDesignPlanDate> Scan(string gameProjectPath)
    {
        if (string.IsNullOrWhiteSpace(gameProjectPath)) return [];
        var root = ResolveDirectory(gameProjectPath);
        if (!Directory.Exists(root)) return [];

        return Directory.EnumerateDirectories(root)
            .Select(path => new DirectoryInfo(path))
            .Where(directory => IsValidDateDirectory(directory.Name))
            .Select(directory => new IndexedDesignPlanDate(
                directory.Name,
                OrderDocuments(Directory.EnumerateFiles(directory.FullName, "*.md", SearchOption.TopDirectoryOnly)
                    .Select(path => CreateDocument(root, directory.Name, path)))))
            .Where(date => date.Documents.Count > 0)
            .OrderByDescending(date => date.Date, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<IndexedDesignPlanDocument> OrderDocuments(
        IEnumerable<IndexedDesignPlanDocument> documents) =>
        documents
            .OrderBy(document => GetStateRank(document.State))
            .ThenBy(document => GetPriorityRank(document.Priority))
            .ThenBy(document => document.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(document => document.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IndexedDesignPlanDocument? FindLinkedDocument(
        IEnumerable<IndexedDesignPlanDocument> documents,
        Uri linkUri)
    {
        if (!linkUri.IsAbsoluteUri || !linkUri.IsFile) return null;

        string linkedPath;
        try
        {
            linkedPath = Path.GetFullPath(linkUri.LocalPath);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }

        return documents.FirstOrDefault(document =>
            Path.GetFullPath(document.FullPath).Equals(linkedPath, StringComparison.OrdinalIgnoreCase));
    }

    public static DesignPlanDocumentContent LoadContent(string path)
    {
        var content = ParseContent(File.ReadAllText(path), Path.GetFileNameWithoutExtension(path));
        return content with
        {
            PlanPath = ResolveAssociatedDocumentPath(path, content.PlanPath),
            ReviewPath = ResolveAssociatedDocumentPath(path, content.ReviewPath),
        };
    }

    public static DesignPlanDocumentContent ParseContent(string markdown, string fallbackTitle)
    {
        var match = StrictHeaderPattern().Match(markdown ?? string.Empty);
        if (!match.Success)
            return new DesignPlanDocumentContent(fallbackTitle, null, null, null, null, markdown ?? string.Empty);

        var title = match.Groups["title"].Value.Trim();
        return new DesignPlanDocumentContent(
            string.IsNullOrWhiteSpace(title) ? fallbackTitle : title,
            EmptyToNull(match.Groups["state"].Value),
            EmptyToNull(match.Groups["priority"].Value),
            EmptyToNull(match.Groups["plan"].Value),
            EmptyToNull(match.Groups["review"].Value),
            (markdown ?? string.Empty)[match.Length..]);
    }

    public static string? ResolveAssociatedDocumentPath(string designPlanPath, string? associatedPath)
    {
        if (string.IsNullOrWhiteSpace(designPlanPath) || string.IsNullOrWhiteSpace(associatedPath)) return null;

        try
        {
            var value = associatedPath.Trim();
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                return uri.IsFile ? Path.GetFullPath(uri.LocalPath) : null;

            var normalized = value.Replace('/', Path.DirectorySeparatorChar);
            if (Path.IsPathFullyQualified(normalized)) return Path.GetFullPath(normalized);

            var documentDirectory = Path.GetDirectoryName(Path.GetFullPath(designPlanPath));
            if (documentDirectory is null) return null;

            var baseDirectory = documentDirectory;
            if (normalized.Equals("AutoDoc", StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("AutoDoc" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                for (var directory = new DirectoryInfo(documentDirectory); directory is not null; directory = directory.Parent)
                {
                    if (!directory.Name.Equals("AutoDoc", StringComparison.OrdinalIgnoreCase)) continue;
                    if (directory.Parent is not null) baseDirectory = directory.Parent.FullName;
                    break;
                }
            }

            return Path.GetFullPath(normalized, baseDirectory);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    private static IndexedDesignPlanDocument CreateDocument(string root, string date, string path)
    {
        var fullPath = Path.GetFullPath(path);
        var fileName = Path.GetFileName(fullPath);
        var fallbackTitle = Path.GetFileNameWithoutExtension(fileName);
        var (title, state, priority) = ReadHeader(fullPath, fallbackTitle);
        return new IndexedDesignPlanDocument(
            fullPath,
            Path.GetRelativePath(root, fullPath).Replace('\\', '/'),
            date,
            fileName,
            title,
            state,
            priority);
    }

    private static (string Title, string? State, string? Priority) ReadHeader(string path, string fallbackTitle)
    {
        try
        {
            using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
            var title = ReadHeaderValue(reader.ReadLine(), "title:");
            var state = ReadHeaderValue(reader.ReadLine(), "state:");
            var priority = ReadHeaderValue(reader.ReadLine(), "priority:");
            return (string.IsNullOrWhiteSpace(title) ? fallbackTitle : title, state, priority);
        }
        catch (IOException)
        {
            return (fallbackTitle, null, null);
        }
        catch (UnauthorizedAccessException)
        {
            return (fallbackTitle, null, null);
        }
    }

    private static string? ReadHeaderValue(string? line, string prefix) =>
        line is not null && line.StartsWith(prefix + " ", StringComparison.Ordinal)
            ? line[(prefix.Length + 1)..].Trim()
            : null;

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsValidDateDirectory(string name) =>
        DateOnly.TryParseExact(name, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    private static int GetStateRank(string? state) => state?.Trim() switch
    {
        var value when value?.Equals("Todo", StringComparison.OrdinalIgnoreCase) == true => 0,
        var value when value?.Equals("In Progress", StringComparison.OrdinalIgnoreCase) == true => 1,
        var value when value?.Equals("In Design", StringComparison.OrdinalIgnoreCase) == true => 2,
        var value when value?.Equals("Warning", StringComparison.OrdinalIgnoreCase) == true => 3,
        var value when value?.Equals("Completed", StringComparison.OrdinalIgnoreCase) == true => 4,
        _ => int.MaxValue,
    };

    private static int GetPriorityRank(string? priority) => priority?.Trim() switch
    {
        var value when value?.Equals("P0", StringComparison.OrdinalIgnoreCase) == true => 0,
        var value when value?.Equals("P1", StringComparison.OrdinalIgnoreCase) == true => 1,
        var value when value?.Equals("P2", StringComparison.OrdinalIgnoreCase) == true => 2,
        _ => int.MaxValue,
    };

    [GeneratedRegex(@"\A(?:\uFEFF)?title: (?<title>[^\r\n]*)(?:\r\n|\n|\r)state: (?<state>[^\r\n]*)(?:\r\n|\n|\r)priority: (?<priority>[^\r\n]*)(?:(?:\r\n|\n|\r)plan: (?<plan>[^\r\n]*))?(?:(?:\r\n|\n|\r)review: (?<review>[^\r\n]*))?(?:(?:\r\n|\n|\r){1,2}|\z)", RegexOptions.CultureInvariant)]
    private static partial Regex StrictHeaderPattern();
}
