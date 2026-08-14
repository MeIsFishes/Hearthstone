using Microsoft.Win32;
using System.IO;
using System.Windows;
using BbxEditor.Contracts;
using BbxEditor.Wpf.Views;

namespace BbxEditor.Wpf.Services;

public enum ExternalFileChangeChoice
{
    KeepLocal,
    Reload,
}

public delegate Task<IReadOnlyList<string>> VectorNameRanker(
    string query, IReadOnlyCollection<string> candidateNames, CancellationToken cancellationToken);

public interface IDialogService
{
    string? OpenDocumentFile(string? initialPath);
    string? SaveDocumentFile(string? initialPath, string extension, string? suggestedFileName = null);
    string? SelectFolder(string? initialPath, string? title = null);
    TaskDefinition? SelectTask(IReadOnlyList<TaskDefinition> tasks, string title, string description, VectorNameRanker? vectorRanker = null);
    bool Confirm(string title, string message);
    ExternalFileChangeChoice ResolveExternalFileChange(string filePath);
    void Show(string title, string message, bool error = false);
}

public sealed class DialogService : IDialogService
{
    public string? OpenDocumentFile(string? initialPath)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "BbxEditor documents (*.editor.json;*.csv;*.asset)|*.editor.json;*.csv;*.asset|Task documents (*.editor.json)|*.editor.json|CSV files (*.csv)|*.csv|BbxScriptableObject assets (*.asset)|*.asset|All files (*.*)|*.*",
            CheckFileExists = true,
        };
        SetInitialDirectory(dialog, initialPath);
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? SaveDocumentFile(string? initialPath, string extension, string? suggestedFileName = null)
    {
        var normalizedExtension = extension.StartsWith('.') ? extension : "." + extension;
        var filter = normalizedExtension.Equals(".editor.json", StringComparison.OrdinalIgnoreCase)
            ? "Task documents (*.editor.json)|*.editor.json"
            : normalizedExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                ? "CSV files (*.csv)|*.csv"
                : "BbxScriptableObject assets (*.asset)|*.asset";
        var dialog = new SaveFileDialog
        {
            Filter = filter,
            AddExtension = true,
            DefaultExt = normalizedExtension,
            FileName = !string.IsNullOrWhiteSpace(suggestedFileName)
                ? suggestedFileName
                : string.IsNullOrWhiteSpace(initialPath) ? "NewDocument" + normalizedExtension : Path.GetFileName(initialPath),
        };
        SetInitialDirectory(dialog, initialPath);
        return dialog.ShowDialog() == true ? NormalizePath(dialog.FileName, normalizedExtension) : null;
    }

    public string? SelectFolder(string? initialPath, string? title = null)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title ?? "Select the Directory Containing BbxEditor Metadata Exported by Unity",
            InitialDirectory = Directory.Exists(initialPath) ? initialPath : null,
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public TaskDefinition? SelectTask(IReadOnlyList<TaskDefinition> tasks, string title, string description, VectorNameRanker? vectorRanker = null)
    {
        var dialog = new TaskSelectionWindow(tasks, title, description, vectorRanker)
        {
            Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                    ?? System.Windows.Application.Current.MainWindow,
        };
        return dialog.ShowDialog() == true ? dialog.SelectedTask : null;
    }

    public bool Confirm(string title, string message) =>
        MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public ExternalFileChangeChoice ResolveExternalFileChange(string filePath)
    {
        var dialog = new ExternalFileChangeWindow(filePath)
        {
            Owner = System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
                    ?? System.Windows.Application.Current.MainWindow,
        };
        _ = dialog.ShowDialog();
        return dialog.Choice;
    }

    public void Show(string title, string message, bool error = false) =>
        MessageBox.Show(message, title, MessageBoxButton.OK, error ? MessageBoxImage.Error : MessageBoxImage.Information);

    private static void SetInitialDirectory(FileDialog dialog, string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        var directory = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
        if (Directory.Exists(directory)) dialog.InitialDirectory = directory;
    }

    private static string NormalizePath(string path, string extension)
    {
        if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) return path;
        if (extension == ".editor.json" && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) path = path[..^5];
        return path + extension;
    }
}
