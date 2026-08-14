using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BbxEditor.Contracts;
using BbxEditor.Infrastructure;
using BbxEditor.Wpf.Services;

namespace BbxEditor.Wpf.Views;

public partial class TaskSelectionWindow : Window
{
    private readonly IReadOnlyList<TaskDefinition> _allTasks;
    private readonly VectorNameRanker? _vectorRanker;
    private CancellationTokenSource? _searchCancellation;
    private int _searchVersion;

    public TaskSelectionWindow(IReadOnlyList<TaskDefinition> tasks, string title, string description, VectorNameRanker? vectorRanker = null)
    {
        _allTasks = tasks.OrderBy(task => task.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToArray();
        _vectorRanker = vectorRanker;
        DialogTitle = title;
        DialogDescription = description;
        InitializeComponent();
        DataContext = this;
        RefreshTasks();
        Loaded += (_, _) => SearchBox.Focus();
    }

    public string DialogTitle { get; }
    public string DialogDescription { get; }
    public ObservableCollection<TaskDefinition> FilteredTasks { get; } = [];
    public TaskDefinition? SelectedTask { get; private set; }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => _ = RefreshTasksAsync();

    private void RefreshTasks()
    {
        if (SearchBox is null || TaskList is null) return;
        var filter = SearchBox.Text.Trim();
        ReplaceTasks(FindLiteralMatches(_allTasks, filter));
    }

    private async Task RefreshTasksAsync()
    {
        if (SearchBox is null || TaskList is null) return;
        var version = ++_searchVersion;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var cancellationToken = _searchCancellation.Token;
        var filter = SearchBox.Text.Trim();
        var literalResults = FindLiteralMatches(_allTasks, filter);
        ReplaceTasks(literalResults);
        if (filter.Length == 0 || _vectorRanker is null) return;

        try
        {
            await Task.Delay(120, cancellationToken);
            var candidateNames = _allTasks.Select(GetVectorName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var rankedNames = await _vectorRanker(filter, candidateNames, cancellationToken);
            if (version != _searchVersion || cancellationToken.IsCancellationRequested ||
                !SearchBox.Text.Trim().Equals(filter, StringComparison.Ordinal)) return;
            ReplaceTasks(MergeVectorResults(_allTasks, literalResults, rankedNames));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // Literal search remains usable if the optional vector query fails.
        }
    }

    private void ReplaceTasks(IReadOnlyList<TaskDefinition> tasks)
    {
        var selectedType = (TaskList.SelectedItem as TaskDefinition)?.FullTypeName;
        FilteredTasks.Clear();
        foreach (var task in tasks) FilteredTasks.Add(task);
        if (selectedType is not null)
            TaskList.SelectedItem = FilteredTasks.FirstOrDefault(task => task.FullTypeName.Equals(selectedType, StringComparison.Ordinal));
        if (TaskList.SelectedItem is null && FilteredTasks.Count == 1) TaskList.SelectedIndex = 0;
    }

    internal static IReadOnlyList<TaskDefinition> FindLiteralMatches(IReadOnlyList<TaskDefinition> tasks, string filter)
    {
        if (filter.Length == 0) return tasks.ToArray();
        var normalizedQuery = VectorSearchNameNormalizer.NormalizeQuery(filter);
        var exact = tasks.Where(task =>
                task.DisplayName.Equals(filter, StringComparison.CurrentCultureIgnoreCase) ||
                task.TypeName.Equals(filter, StringComparison.OrdinalIgnoreCase) ||
                task.FullTypeName.Equals(filter, StringComparison.OrdinalIgnoreCase) ||
                GetVectorName(task).Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var exactTypes = exact.Select(task => task.FullTypeName).ToHashSet(StringComparer.Ordinal);
        return exact.Concat(tasks.Where(task => !exactTypes.Contains(task.FullTypeName) && Matches(task, filter))).ToArray();
    }

    internal static IReadOnlyList<TaskDefinition> MergeVectorResults(
        IReadOnlyList<TaskDefinition> allTasks,
        IReadOnlyList<TaskDefinition> literalResults,
        IReadOnlyList<string> rankedNames)
    {
        var literalTypes = literalResults.Select(task => task.FullTypeName).ToHashSet(StringComparer.Ordinal);
        var ranks = rankedNames.Select((name, rank) => (name, rank))
            .GroupBy(item => item.name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Min(item => item.rank), StringComparer.OrdinalIgnoreCase);
        var vectorResults = allTasks.Where(task => !literalTypes.Contains(task.FullTypeName) && ranks.ContainsKey(GetVectorName(task)))
            .OrderBy(task => ranks[GetVectorName(task)])
            .ThenBy(task => task.DisplayName, StringComparer.CurrentCultureIgnoreCase);
        return literalResults.Concat(vectorResults).ToArray();
    }

    internal static string GetVectorName(TaskDefinition task) => VectorSearchNameNormalizer.NormalizeTaskName(task.TypeName);

    private static bool Matches(TaskDefinition task, string filter) =>
        filter.Length == 0 ||
        task.DisplayName.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
        task.TypeName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        task.FullTypeName.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        task.Tags.Any(tag => tag.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
        (task.Comment?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);

    private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedTask = TaskList.SelectedItem as TaskDefinition;
        CreateButton.IsEnabled = SelectedTask is not null;
    }

    private void TaskList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TaskList.SelectedItem is TaskDefinition) ConfirmSelection();
    }

    private void CreateButton_Click(object sender, RoutedEventArgs e) => ConfirmSelection();

    private void ConfirmSelection()
    {
        if (SelectedTask is null) return;
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
