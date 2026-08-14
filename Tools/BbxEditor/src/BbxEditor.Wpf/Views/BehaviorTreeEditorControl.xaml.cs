using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BbxEditor.Domain;
using BbxEditor.Wpf.ViewModels;

namespace BbxEditor.Wpf.Views;

public partial class BehaviorTreeEditorControl : UserControl
{
    private CancellationTokenSource? _findCancellation;
    private IReadOnlyList<BehaviorNode> _findResults = [];
    private string _findQuery = string.Empty;
    private int _findIndexVersion = -1;
    private int _findResultIndex = -1;

    public BehaviorTreeEditorControl()
    {
        InitializeComponent();
    }

    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            OpenFind();
            e.Handled = true;
        }
    }

    private void OpenFind()
    {
        FindBar.Visibility = Visibility.Visible;
        _ = Dispatcher.BeginInvoke(() =>
        {
            FindBox.Focus();
            FindBox.SelectAll();
        });
    }

    private void FindBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        CancelFindRequest();
        _findQuery = string.Empty;
        _findResults = [];
        _findResultIndex = -1;
        FindResultText.Text = string.Empty;
        if (DataContext is BehaviorTreeDocumentViewModel viewModel) viewModel.HighlightedSearchNode = null;
    }

    private void FindBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _ = FindNextAsync();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseFind();
            e.Handled = true;
        }
    }

    private void FindNext_Click(object sender, RoutedEventArgs e) => _ = FindNextAsync();

    private async Task FindNextAsync()
    {
        if (DataContext is not BehaviorTreeDocumentViewModel viewModel) return;
        var query = FindBox.Text.Trim();
        if (query.Length == 0)
        {
            FindResultText.Text = "Enter a search term";
            return;
        }

        var needsRefresh = !_findQuery.Equals(query, StringComparison.Ordinal) ||
                           _findIndexVersion != viewModel.NodeSearchIndexVersion;
        if (needsRefresh)
        {
            CancelFindRequest();
            _findCancellation = new CancellationTokenSource();
            var cancellationToken = _findCancellation.Token;
            try
            {
                var results = await viewModel.FindNodesAsync(query, cancellationToken);
                if (cancellationToken.IsCancellationRequested || !FindBox.Text.Trim().Equals(query, StringComparison.Ordinal)) return;
                _findQuery = query;
                _findIndexVersion = viewModel.NodeSearchIndexVersion;
                _findResults = results;
                _findResultIndex = -1;
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        if (_findResults.Count == 0)
        {
            viewModel.HighlightedSearchNode = null;
            FindResultText.Text = "No matches";
            return;
        }

        _findResultIndex = (_findResultIndex + 1) % _findResults.Count;
        var node = _findResults[_findResultIndex];
        viewModel.HighlightedSearchNode = node;
        viewModel.SelectedNode = node;
        Canvas.CenterOnNode(node);
        FindResultText.Text = $"{_findResultIndex + 1} of {_findResults.Count}";
    }

    private void CloseFind_Click(object sender, RoutedEventArgs e) => CloseFind();

    private void CloseFind()
    {
        CancelFindRequest();
        FindBar.Visibility = Visibility.Collapsed;
        if (DataContext is BehaviorTreeDocumentViewModel viewModel) viewModel.HighlightedSearchNode = null;
        Canvas.Focus();
    }

    private void CancelFindRequest()
    {
        var cancellation = Interlocked.Exchange(ref _findCancellation, null);
        cancellation?.Cancel();
        cancellation?.Dispose();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        CancelFindRequest();
        if (DataContext is BehaviorTreeDocumentViewModel viewModel) viewModel.HighlightedSearchNode = null;
    }
}
