using BbxEditor.Contracts;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Application;

public sealed record CsvAssociationTarget(
    string AssociationName,
    string TableName,
    CsvTypeMetadata? Metadata,
    IndexedProjectFile? File,
    string? UnavailableReason)
{
    public bool CanOpen => File is not null && UnavailableReason is null;
}

public static class CsvAssociationTargetResolver
{
    public static IReadOnlyList<CsvAssociationTarget> Resolve(
        CsvDocument document,
        BbxMetadataCatalog metadataCatalog,
        IReadOnlyList<IndexedProjectFile> indexedFiles,
        bool indexReady)
    {
        if (document.HeaderComments.Count < 2)
        {
            return [new CsvAssociationTarget(string.Empty, string.Empty, null, null,
                "The CSV association comment is unavailable.")];
        }
        if (!CsvAssociationContract.TryParse(document.HeaderComments[1], out var associationNames, out var parseError))
        {
            return [new CsvAssociationTarget(string.Empty, string.Empty, null, null, parseError)];
        }
        if (associationNames.Count == 0) return [];

        var targets = new List<CsvAssociationTarget>();
        foreach (var associationName in associationNames)
        {
            var metadata = metadataCatalog.FindCsvByTableName(associationName);
            var tableNames = metadata is null
                ? new[] { associationName }
                : metadata.TableNames.Where(name => !string.IsNullOrWhiteSpace(name))
                    .Append(associationName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

            foreach (var tableName in tableNames)
            {
                if (!indexReady)
                {
                    targets.Add(new CsvAssociationTarget(associationName, tableName, metadata, null,
                        "The project file index is not ready."));
                    continue;
                }

                var files = indexedFiles
                    .Where(file => file.Kind == ProjectFileKind.Csv &&
                                   Path.GetFileNameWithoutExtension(file.FileName).Equals(tableName, StringComparison.OrdinalIgnoreCase) &&
                                   !IsCurrentFile(document.FilePath, file.FullPath))
                    .ToArray();
                if (metadata is null)
                {
                    if (files.Length == 0)
                    {
                        targets.Add(new CsvAssociationTarget(associationName, tableName, null, null,
                            $"No exported CSV metadata or indexed file was found for '{associationName}'."));
                    }
                    else
                    {
                        targets.AddRange(files.Select(file => new CsvAssociationTarget(associationName, tableName, null, file,
                            $"No exported CSV metadata was found for '{associationName}'.")));
                    }
                    continue;
                }

                if (files.Length == 0)
                {
                    targets.Add(new CsvAssociationTarget(associationName, tableName, metadata, null,
                        $"No indexed CSV file was found for table '{tableName}'."));
                    continue;
                }
                targets.AddRange(files.Select(file =>
                    new CsvAssociationTarget(associationName, tableName, metadata, file, null)));
            }
        }

        return targets
            .DistinctBy(target => (
                target.TableName.ToUpperInvariant(),
                target.File?.FullPath.ToUpperInvariant() ?? string.Empty,
                target.UnavailableReason ?? string.Empty))
            .OrderBy(TargetOrder)
            .ThenBy(target => target.File?.ModName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(target => target.TableName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(target => target.File?.RelativePath ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int TargetOrder(CsvAssociationTarget target)
    {
        if (target.File is null) return 2;
        return target.File.ModName.Equals("Native", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
    }

    private static bool IsCurrentFile(string? currentPath, string candidatePath) =>
        !string.IsNullOrWhiteSpace(currentPath) &&
        Path.GetFullPath(currentPath).Equals(Path.GetFullPath(candidatePath), StringComparison.OrdinalIgnoreCase);
}
