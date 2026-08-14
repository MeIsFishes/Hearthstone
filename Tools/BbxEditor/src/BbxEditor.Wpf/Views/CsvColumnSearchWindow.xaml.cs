using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BbxEditor.Wpf.Views;

internal delegate Task<IReadOnlyList<string>> CsvValueRanker(
    string query,
    IReadOnlyCollection<string> candidateValues,
    CancellationToken cancellationToken);

public sealed record CsvColumnSearchResult(int RowIndex, string Value)
{
    public string RowLabel => $"Row {RowIndex + 1}";
    public string DisplayValue => Value.Length == 0 ? "(empty)" : Value;
}

public partial class CsvColumnSearchWindow : Window
{
    private readonly IReadOnlyList<CsvColumnSearchResult> _allRows;
    private readonly CsvValueRanker? _vectorRanker;
    private CancellationTokenSource? _searchCancellation;
    private int _searchVersion;

    internal CsvColumnSearchWindow(
        string columnName,
        IReadOnlyList<CsvColumnSearchResult> rows,
        bool vectorSupported,
        CsvValueRanker? vectorRanker)
    {
        _allRows = rows;
        _vectorRanker = vectorRanker;
        DialogTitle = $"Turn To: {columnName}";
        SearchDescription = vectorSupported
            ? "Exact and literal matches appear first. Semantic matches follow when vector search is ready."
            : "Literal search is available. Semantic search requires exported String metadata for this column.";
        InitializeComponent();
        DataContext = this;
        ReplaceRows(_allRows);
        Loaded += (_, _) => SearchBox.Focus();
    }

    public string DialogTitle { get; }
    public string SearchDescription { get; }
    public ObservableCollection<CsvColumnSearchResult> FilteredRows { get; } = [];
    internal CsvColumnSearchResult? SelectedResult { get; private set; }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _ = RefreshRowsAsync();

    private async Task RefreshRowsAsync()
    {
        if (SearchBox is null || ResultList is null) return;
        var version = ++_searchVersion;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var cancellationToken = _searchCancellation.Token;
        var query = SearchBox.Text.Trim();
        var literalRows = FindLiteralMatches(_allRows, query);
        ReplaceRows(literalRows);
        if (query.Length == 0 || _vectorRanker is null) return;

        try
        {
            await Task.Delay(120, cancellationToken);
            var candidateValues = _allRows.Select(row => row.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var rankedValues = await _vectorRanker(query, candidateValues, cancellationToken);
            if (version != _searchVersion || cancellationToken.IsCancellationRequested ||
                !SearchBox.Text.Trim().Equals(query, StringComparison.Ordinal)) return;
            ReplaceRows(MergeVectorResults(_allRows, literalRows, rankedValues));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // Literal search remains available when vector indexing or querying fails.
        }
    }

    internal static IReadOnlyList<CsvColumnSearchResult> FindLiteralMatches(
        IReadOnlyList<CsvColumnSearchResult> rows,
        string query)
    {
        if (query.Length == 0) return rows.ToArray();
        var exact = rows.Where(row => row.Value.Equals(query, StringComparison.CurrentCultureIgnoreCase)).ToArray();
        var exactIndices = exact.Select(row => row.RowIndex).ToHashSet();
        return exact.Concat(rows.Where(row => !exactIndices.Contains(row.RowIndex) &&
            row.Value.Contains(query, StringComparison.CurrentCultureIgnoreCase))).ToArray();
    }

    internal static IReadOnlyList<CsvColumnSearchResult> MergeVectorResults(
        IReadOnlyList<CsvColumnSearchResult> allRows,
        IReadOnlyList<CsvColumnSearchResult> literalRows,
        IReadOnlyList<string> rankedValues)
    {
        var literalIndices = literalRows.Select(row => row.RowIndex).ToHashSet();
        var ranks = rankedValues.Select((value, rank) => (value, rank))
            .GroupBy(item => item.value, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Min(item => item.rank), StringComparer.OrdinalIgnoreCase);
        var semanticRows = allRows.Where(row => !literalIndices.Contains(row.RowIndex) && ranks.ContainsKey(row.Value))
            .OrderBy(row => ranks[row.Value])
            .ThenBy(row => row.RowIndex);
        return literalRows.Concat(semanticRows).ToArray();
    }

    private void ReplaceRows(IReadOnlyList<CsvColumnSearchResult> rows)
    {
        var selectedIndex = (ResultList.SelectedItem as CsvColumnSearchResult)?.RowIndex;
        FilteredRows.Clear();
        foreach (var row in rows) FilteredRows.Add(row);
        if (selectedIndex is not null)
            ResultList.SelectedItem = FilteredRows.FirstOrDefault(row => row.RowIndex == selectedIndex);
        if (ResultList.SelectedItem is null && FilteredRows.Count == 1) ResultList.SelectedIndex = 0;
    }

    private void ResultList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedResult = ResultList.SelectedItem as CsvColumnSearchResult;
        GoToButton.IsEnabled = SelectedResult is not null;
    }

    private void ResultList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ResultList.SelectedItem is CsvColumnSearchResult) ConfirmSelection();
    }

    private void GoToButton_Click(object sender, RoutedEventArgs e) => ConfirmSelection();

    private void ConfirmSelection()
    {
        if (SelectedResult is null) return;
        DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;
        base.OnClosed(e);
    }
}
