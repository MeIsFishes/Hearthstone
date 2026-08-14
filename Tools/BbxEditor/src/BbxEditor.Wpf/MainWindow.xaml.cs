using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BbxEditor.Wpf.Services;
using BbxEditor.Wpf.ViewModels;
using BbxEditor.Wpf.Views;

namespace BbxEditor.Wpf;

public partial class MainWindow : Window
{
    private Views.LogDetailsWindow? _logDetailsWindow;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler(MainWindow_PreviewMouseDown), true);
        var viewModel = new MainViewModel(new DialogService());
        DataContext = viewModel;
        Inspector.FieldChanged += (_, _) =>
        {
            if (viewModel.CurrentDocument is not null)
            {
                viewModel.CurrentDocument.Document.IsDirty = true;
            }
        };
    }

    private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var focusedElement = Keyboard.FocusedElement as DependencyObject;
        var csvEditor = FindVisualParent<CsvEditorControl>(focusedElement);
        if (csvEditor is null) return;

        var clickedElement = e.OriginalSource as DependencyObject;
        if (IsSelfOrDescendantOf(clickedElement, focusedElement)) return;
        csvEditor.CommitEditingAndClearFocus();
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!IsSaveShortcut(e.Key, e.KeyboardDevice.Modifiers)) return;
        e.Handled = true;

        FindVisualParent<CsvEditorControl>(Keyboard.FocusedElement as DependencyObject)
            ?.CommitEditingAndClearFocus();
        if (DataContext is MainViewModel viewModel && viewModel.SaveCommand.CanExecute(null))
            viewModel.SaveCommand.Execute(null);
    }

    internal static bool IsSaveShortcut(Key key, ModifierKeys modifiers) =>
        key == Key.S && modifiers == ModifierKeys.Control;

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var window = new Views.SettingsWindow
        {
            Owner = this,
            DataContext = DataContext,
        };
        _ = window.ShowDialog();
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        if (_logDetailsWindow is not null)
        {
            if (_logDetailsWindow.WindowState == WindowState.Minimized) _logDetailsWindow.WindowState = WindowState.Normal;
            _logDetailsWindow.Activate();
            return;
        }

        if (DataContext is not MainViewModel viewModel) return;
        _logDetailsWindow = new Views.LogDetailsWindow(viewModel.Log) { Owner = this };
        _logDetailsWindow.Closed += (_, _) => _logDetailsWindow = null;
        _logDetailsWindow.Show();
    }

    private void DocumentTabs_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        var clickedButton = FindVisualParent<Button>(e.OriginalSource as DependencyObject);
        if (clickedButton?.DataContext is DocumentViewModel buttonDocument &&
            ReferenceEquals(clickedButton.Command, buttonDocument.CloseCommand)) return;
        if (FindVisualParent<MenuItem>(e.OriginalSource as DependencyObject) is not null) return;
        if (DataContext is not MainViewModel viewModel) return;
        var clickedTab = FindVisualParent<TabItem>(e.OriginalSource as DependencyObject);
        if (clickedTab?.DataContext is DocumentViewModel document) viewModel.PinPreviewDocument(document);
        else viewModel.PinCurrentPreviewDocument();
    }

    private static T? FindVisualParent<T>(DependencyObject? element) where T : DependencyObject
    {
        for (var current = element; current is not null; current = GetParent(current))
            if (current is T match) return match;
        return null;
    }

    private static bool IsSelfOrDescendantOf(DependencyObject? element, DependencyObject? ancestor)
    {
        if (ancestor is null) return false;
        for (var current = element; current is not null; current = GetParent(current))
            if (ReferenceEquals(current, ancestor)) return true;
        return false;
    }

    private static DependencyObject? GetParent(DependencyObject element)
    {
        if (element is FrameworkContentElement contentElement) return contentElement.Parent;
        return element is Visual ? VisualTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (DataContext is MainViewModel viewModel) _ = viewModel.InitializeAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable) disposable.Dispose();
        base.OnClosed(e);
    }
}
