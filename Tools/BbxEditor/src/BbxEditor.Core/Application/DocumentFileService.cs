using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Application;

public sealed class DocumentFileService
{
    public OperationResult<EditorDocument> Open(string filePath, TaskCatalog catalog)
    {
        try
        {
            var result = LegacyEditorImporter.Import(filePath);

            if (result.Value is TaskDocument taskDocument)
            {
                result.Diagnostics.AddRange(TaskReconciler.Reconcile(taskDocument, catalog));
            }
            return result;
        }
        catch (Exception exception)
        {
            var result = new OperationResult<EditorDocument>();
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "DOCUMENT_OPEN_FAILED", exception.Message, filePath));
            return result;
        }
    }

    public OperationResult<string> Save(TaskDocument document, TaskCatalog catalog, string? requestedPath = null)
    {
        var result = new OperationResult<string>();
        var editorPath = NormalizeEditorPath(requestedPath ?? document.FilePath);
        if (editorPath is null)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "SAVE_PATH_REQUIRED", "No save path has been selected."));
            return result;
        }

        var export = RuntimeExporter.Export(document, catalog);
        result.Diagnostics.AddRange(export.Diagnostics);
        if (export.Value is null || result.Diagnostics.Any(item => item.Severity == DiagnosticSeverity.Error))
        {
            return result;
        }

        var runtimePath = editorPath[..^".editor.json".Length] + ".json";
        var editorJson = LegacyEditorWriter.Serialize(document, catalog, editorPath);
        var runtimeJson = LegacyRuntimeWriter.Serialize(export.Value);
        try
        {
            AtomicWritePair(editorPath, editorJson, runtimePath, runtimeJson);
            document.FilePath = editorPath;
            document.IsDirty = false;
            result.Value = editorPath;
        }
        catch (Exception exception)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "SAVE_FAILED", exception.Message, editorPath));
        }
        return result;
    }

    private static string? NormalizeEditorPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var full = Path.GetFullPath(path);
        if (full.EndsWith(".editor.json", StringComparison.OrdinalIgnoreCase)) return full;
        if (full.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) full = full[..^5];
        return full + ".editor.json";
    }

    private static void AtomicWritePair(string editorPath, string editorContent, string runtimePath, string runtimeContent)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(editorPath) ?? ".");
        var token = Guid.NewGuid().ToString("N");
        var editorTemp = editorPath + "." + token + ".tmp";
        var runtimeTemp = runtimePath + "." + token + ".tmp";
        var editorBackup = editorPath + "." + token + ".bak";
        var runtimeBackup = runtimePath + "." + token + ".bak";
        var editorExisted = File.Exists(editorPath);
        var runtimeExisted = File.Exists(runtimePath);
        try
        {
            File.WriteAllText(editorTemp, editorContent);
            File.WriteAllText(runtimeTemp, runtimeContent);
            if (File.Exists(editorPath)) File.Copy(editorPath, editorBackup);
            if (File.Exists(runtimePath)) File.Copy(runtimePath, runtimeBackup);
            File.Move(editorTemp, editorPath, true);
            File.Move(runtimeTemp, runtimePath, true);
        }
        catch
        {
            if (File.Exists(editorBackup)) File.Move(editorBackup, editorPath, true);
            else if (!editorExisted && File.Exists(editorPath)) File.Delete(editorPath);
            if (File.Exists(runtimeBackup)) File.Move(runtimeBackup, runtimePath, true);
            else if (!runtimeExisted && File.Exists(runtimePath)) File.Delete(runtimePath);
            throw;
        }
        finally
        {
            foreach (var path in new[] { editorTemp, runtimeTemp, editorBackup, runtimeBackup })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
