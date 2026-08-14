using BbxEditor.Diagnostics;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Application;

public sealed class EditorCatalog
{
    public EditorCatalog(TaskCatalog tasks, BbxMetadataCatalog metadata)
    {
        Tasks = tasks;
        Metadata = metadata;
    }

    public TaskCatalog Tasks { get; }
    public BbxMetadataCatalog Metadata { get; }
}

public sealed class WorkspaceDocumentService
{
    private readonly DocumentFileService _taskDocuments = new();

    public OperationResult<EditorDocument> Open(string path, EditorCatalog catalog)
    {
        if (path.EndsWith(".editor.json", StringComparison.OrdinalIgnoreCase)) return _taskDocuments.Open(path, catalog.Tasks);
        if (path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)) return Upcast(CsvDocumentCodec.Open(path, catalog.Metadata.FindCsvForPath(path)));
        if (path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var content = File.ReadAllText(path);
                var guid = ScriptableObjectDocumentCodec.ReadScriptGuid(content);
                return Upcast(ScriptableObjectDocumentCodec.Open(path, catalog.Metadata.FindScriptableObjectByGuid(guid)));
            }
            catch (Exception exception)
            {
                return Failed("SCRIPTABLE_OBJECT_OPEN_FAILED", exception.Message, path);
            }
        }
        return Failed("DOCUMENT_TYPE_UNSUPPORTED", "BbxEditor supports .editor.json, .csv, and exported BbxScriptableObject .asset files.", path);
    }

    public OperationResult<string> Save(EditorDocument document, EditorCatalog catalog, string path) => document switch
    {
        TaskDocument task => _taskDocuments.Save(task, catalog.Tasks, path),
        CsvDocument csv => CsvDocumentCodec.Save(csv, EnsureExtension(path, ".csv")),
        ScriptableObjectDocument scriptableObject => ScriptableObjectDocumentCodec.Save(scriptableObject, EnsureExtension(path, ".asset")),
        _ => FailedString("DOCUMENT_TYPE_UNSUPPORTED", $"Unsupported document type: {document.GetType().Name}", path),
    };

    private static string EnsureExtension(string path, string extension) => path.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? path : path + extension;

    private static OperationResult<EditorDocument> Upcast<T>(OperationResult<T> source) where T : EditorDocument
    {
        var result = new OperationResult<EditorDocument> { Value = source.Value };
        result.Diagnostics.AddRange(source.Diagnostics);
        return result;
    }

    private static OperationResult<EditorDocument> Failed(string code, string message, string path)
    {
        var result = new OperationResult<EditorDocument>();
        result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, code, message, path));
        return result;
    }

    private static OperationResult<string> FailedString(string code, string message, string path)
    {
        var result = new OperationResult<string>();
        result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, code, message, path));
        return result;
    }
}
