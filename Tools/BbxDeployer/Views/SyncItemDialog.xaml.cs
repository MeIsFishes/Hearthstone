using System.Collections.ObjectModel;
using System.Windows;
using BbxDeployer.Core;
using BbxDeployer.Services;

namespace BbxDeployer.Views;

public partial class SyncItemDialog : Window
{
    private readonly SyncItem _original;
    private readonly ProjectContext _source;
    private readonly DialogService _dialogService;

    public SyncItemDialog(
        SyncItem item,
        ProjectContext source,
        DialogService dialogService)
    {
        InitializeComponent();
        _original = item;
        _source = source;
        _dialogService = dialogService;
        DisplayNameBox.Text = item.DisplayName;
        WhitelistPaths = new ObservableCollection<EditableSyncPath>(
            SyncItemPathExpander.GetConfiguredPaths(item).Select(path =>
                new EditableSyncPath(
                    path.RelativePath,
                    path.ManualExcludePatterns)));
        DataContext = this;
        Loaded += (_, _) =>
        {
            if (WhitelistPathsGrid.SelectedItem is null && WhitelistPaths.Count > 0)
            {
                WhitelistPathsGrid.SelectedIndex = 0;
            }
        };
    }

    public ObservableCollection<EditableSyncPath> WhitelistPaths { get; }

    public SyncItem? Result { get; private set; }

    private EditableSyncPath? SelectedWhitelist =>
        WhitelistPathsGrid.SelectedItem as EditableSyncPath;

    private void AddWhitelistPath_Click(object sender, RoutedEventArgs e)
    {
        var initialDirectory = Directory.Exists(_source.RepositoryRoot)
            ? _source.RepositoryRoot
            : null;
        var selected = _dialogService.SelectFolder(
            "Select Whitelist Directory",
            initialDirectory);
        if (selected is null)
        {
            return;
        }

        try
        {
            AddWhitelist(ToProjectRelativeTemplate(selected));
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message);
        }
    }

    private void RemoveWhitelist_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWhitelist is { } selected)
        {
            WhitelistPaths.Remove(selected);
        }
    }

    private void AddBlacklistPath_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWhitelist is not { } selected
            || !TryGetWhitelistRoot(selected, out var whitelistRoot))
        {
            return;
        }

        var path = _dialogService.SelectFolder(
            "Select Blacklisted Directory",
            Directory.Exists(whitelistRoot) ? whitelistRoot : null);
        if (path is null)
        {
            return;
        }

        try
        {
            if (!PathService.IsSameOrDescendant(path, whitelistRoot))
            {
                _dialogService.ShowError(
                    "The blacklisted directory must be inside the selected whitelist.");
                return;
            }

            var relative = PathService.ToPortableRelativePath(
                whitelistRoot,
                path).Trim('/');
            if (relative.Length == 0)
            {
                _dialogService.ShowError("The whitelist root itself cannot be blacklisted.");
                return;
            }

            AddRule(selected, relative + "/");
        }
        catch (Exception exception)
        {
            _dialogService.ShowError(exception.Message);
        }
    }

    private void RemoveRule_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedWhitelist is { } selected
            && ManualRulesList.SelectedItem is string rule)
        {
            selected.ManualRules.Remove(rule);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DisplayNameBox.Text))
        {
            ShowWarning("Name is required.");
            return;
        }

        if (WhitelistPaths.Count == 0)
        {
            ShowWarning("Add at least one whitelist path.");
            return;
        }

        try
        {
            var normalizedPaths = WhitelistPaths.Select(path => new SyncPathEntry
            {
                RelativePath = NormalizeTemplate(path.RelativePath),
                ManualExcludePatterns = [.. path.ManualRules]
            }).ToList();
            if (normalizedPaths
                .Select(path => path.RelativePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count() != normalizedPaths.Count)
            {
                ShowWarning("Whitelist paths must be unique.");
                return;
            }

            Result = _original.Clone();
            Result.Enabled = true;
            Result.DisplayName = DisplayNameBox.Text.Trim();
            Result.WhitelistPaths = normalizedPaths;
            SyncPathTemplate.ApplyProjectRelativePath(
                Result,
                normalizedPaths[0].RelativePath);
            Result.UseGitIgnoreFiles = true;
            Result.ManualExcludePatterns =
                [.. normalizedPaths[0].ManualExcludePatterns];
            Result.AdditionalIgnoreFiles = [];
            DialogResult = true;
        }
        catch (ArgumentException exception)
        {
            ShowWarning(exception.Message);
        }
    }

    private void AddWhitelist(string normalized)
    {
        if (WhitelistPaths.Any(path => path.RelativePath.Equals(
                normalized,
                StringComparison.OrdinalIgnoreCase)))
        {
            _dialogService.ShowError("That whitelist path is already in the list.");
            return;
        }

        var added = new EditableSyncPath(normalized, []);
        WhitelistPaths.Add(added);
        WhitelistPathsGrid.SelectedItem = added;
    }

    private static void AddRule(EditableSyncPath path, string rule)
    {
        if (!string.IsNullOrWhiteSpace(rule)
            && !path.ManualRules.Contains(rule, StringComparer.OrdinalIgnoreCase))
        {
            path.ManualRules.Add(rule);
        }
    }

    private bool TryGetWhitelistRoot(
        EditableSyncPath path,
        out string whitelistRoot)
    {
        try
        {
            var item = _original.Clone();
            SyncPathTemplate.ApplyProjectRelativePath(item, path.RelativePath);
            var sourceBase = PathService.ResolveBase(_source, item.SourceBase);
            whitelistRoot = PathService.ResolveInside(
                sourceBase,
                item.SourceRelativePath);
            return true;
        }
        catch (Exception exception)
        {
            whitelistRoot = string.Empty;
            _dialogService.ShowError(exception.Message);
            return false;
        }
    }

    private string ToProjectRelativeTemplate(string selected)
    {
        if (Directory.Exists(_source.UnityProjectRoot)
            && PathService.IsSameOrDescendant(
                selected,
                _source.UnityProjectRoot))
        {
            var relative = PathService.ToPortableRelativePath(
                _source.UnityProjectRoot,
                selected).Trim('/');
            if (relative.Length == 0)
            {
                throw new ArgumentException(
                    "Select a directory inside the Unity game folder.");
            }

            return NormalizeTemplate(
                $"{SyncPathTemplate.GameProjectToken}/{relative}");
        }

        if (Directory.Exists(_source.RepositoryRoot)
            && PathService.IsSameOrDescendant(selected, _source.RepositoryRoot))
        {
            var relative = PathService.ToPortableRelativePath(
                _source.RepositoryRoot,
                selected).Trim('/');
            if (relative.Length == 0)
            {
                throw new ArgumentException(
                    "Select a directory inside the project root.");
            }

            return NormalizeTemplate(relative);
        }

        throw new ArgumentException(
            "The whitelist directory must be inside the main project root.");
    }

    private string NormalizeTemplate(string path)
    {
        var item = _original.Clone();
        SyncPathTemplate.ApplyProjectRelativePath(item, path);
        return SyncPathTemplate.ToProjectRelativePath(item);
    }

    private void ShowWarning(string message)
    {
        MessageBox.Show(
            this,
            message,
            "Transfer Directory Group",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}

public sealed class EditableSyncPath
{
    public EditableSyncPath(
        string relativePath,
        IEnumerable<string> manualRules)
    {
        RelativePath = relativePath;
        ManualRules = new ObservableCollection<string>(manualRules);
    }

    public string RelativePath { get; }

    public ObservableCollection<string> ManualRules { get; }
}
