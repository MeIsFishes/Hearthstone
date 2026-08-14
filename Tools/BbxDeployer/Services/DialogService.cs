using System.Windows;
using BbxDeployer.Core;
using BbxDeployer.ViewModels;
using BbxDeployer.Views;
using Microsoft.Win32;

namespace BbxDeployer.Services;

public sealed class DialogService
{
    public string? SelectFolder(string title, string? initialDirectory = null)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            Multiselect = false
        };

        if (!string.IsNullOrWhiteSpace(initialDirectory) && Directory.Exists(initialDirectory))
        {
            dialog.InitialDirectory = initialDirectory;
        }

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public bool Confirm(string message, string title, Window? owner = null)
    {
        var result = owner is null
            ? MessageBox.Show(
                message,
                title,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning)
            : MessageBox.Show(
                owner,
                message,
                title,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);
        return result == MessageBoxResult.OK;
    }

    public void ShowError(string message, string title = "BbxDeployer")
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public ProjectContext? EditProject(ProjectContext project, Window? owner)
    {
        var dialog = new ProjectDialog(project.Clone()) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public SyncItem? EditSyncItem(
        SyncItem item,
        ProjectContext source,
        Window? owner)
    {
        var dialog = new SyncItemDialog(
            item.Clone(),
            source.Clone(),
            this)
        {
            Owner = owner
        };
        return dialog.ShowDialog() == true ? dialog.Result : null;
    }

    public void ShowSettings(MainViewModel viewModel, Window? owner)
    {
        var dialog = new SettingsDialog
        {
            Owner = owner,
            DataContext = viewModel
        };
        dialog.ShowDialog();
    }
}
