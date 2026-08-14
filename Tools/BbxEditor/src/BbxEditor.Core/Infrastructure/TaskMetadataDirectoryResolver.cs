using System.Text.Json;

namespace BbxEditor.Infrastructure;

public static class TaskMetadataDirectoryResolver
{
    public static string Resolve(string metadataDirectory)
    {
        var fullPath = Path.GetFullPath(metadataDirectory);
        var candidates = new[]
        {
            fullPath,
            Path.Combine(fullPath, "Task"),
            Path.Combine(Path.GetDirectoryName(fullPath) ?? fullPath, "ExportedTaskInfo"),
        };

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (ContainsTaskMetadata(candidate)) return candidate;
        }

        var nestedTaskDirectory = Path.Combine(fullPath, "Task");
        return Directory.Exists(nestedTaskDirectory) ? nestedTaskDirectory : fullPath;
    }

    private static bool ContainsTaskMetadata(string directory)
    {
        if (!Directory.Exists(directory)) return false;
        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                var fullType = LegacyJson.ReadFullType(document.RootElement);
                if (fullType?.EndsWith("TaskExportInfo", StringComparison.Ordinal) == true ||
                    fullType?.EndsWith("TaskContextExportInfo", StringComparison.Ordinal) == true ||
                    fullType?.EndsWith("TaskEnumExportInfo", StringComparison.Ordinal) == true)
                {
                    return true;
                }
            }
            catch (JsonException)
            {
                // TaskCatalog reports malformed files after a directory has been selected.
            }
            catch (IOException)
            {
                // A transient read failure should not prevent checking the other candidates.
            }
            catch (UnauthorizedAccessException)
            {
                // TaskCatalog will provide the actionable diagnostic when this candidate is used.
            }
        }
        return false;
    }
}
