using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.IO;
using BbxEditor.Application;
using BbxEditor.Contracts;
using BbxEditor.Infrastructure;
using BbxEditor.Wpf.ViewModels;

namespace BbxEditor.Wpf.Views;

public partial class CsvEditorControl : UserControl
{
    public CsvEditorControl()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs args)
    {
        if (args.NewValue is not CsvDocumentViewModel viewModel) return;
        BuildColumns(viewModel);
        RebuildAssociatedTablesMenu(viewModel);
    }

    private void AssociatedTables_SubmenuOpened(object sender, RoutedEventArgs e)
    {
        if (DataContext is CsvDocumentViewModel viewModel) RebuildAssociatedTablesMenu(viewModel);
    }

    private void RebuildAssociatedTablesMenu(CsvDocumentViewModel viewModel)
    {
        var targets = viewModel.Owner.ResolveAssociatedCsvTargets(viewModel.Csv);
        var availableCount = targets.Count(target => target.CanOpen);
        AssociatedTablesMenuItem.Header = $"Associated Tables ({availableCount})";
        AssociatedTablesMenuItem.Items.Clear();
        if (targets.Count == 0)
        {
            AssociatedTablesMenuItem.IsEnabled = false;
            AssociatedTablesMenuItem.ToolTip = "This CSV does not declare associated tables.";
            return;
        }

        AssociatedTablesMenuItem.IsEnabled = true;
        AssociatedTablesMenuItem.ToolTip = "Open a CSV table declared by the Associated header comment.";
        var baseLabels = targets.Select(FormatAssociatedTargetLabel).ToArray();
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var label = baseLabels[index];
            if (target.File is not null && baseLabels.Count(value => value.Equals(label, StringComparison.OrdinalIgnoreCase)) > 1)
            {
                var parent = Path.GetDirectoryName(target.File.RelativePath.Replace('/', Path.DirectorySeparatorChar))
                    ?.Replace(Path.DirectorySeparatorChar, '/');
                if (!string.IsNullOrWhiteSpace(parent)) label += $" · {parent}";
            }

            var item = new MenuItem
            {
                Header = label,
                IsEnabled = target.CanOpen,
                ToolTip = BuildAssociatedTargetToolTip(target),
            };
            if (target.CanOpen) item.Click += (_, _) => viewModel.Owner.OpenAssociatedCsv(target);
            AssociatedTablesMenuItem.Items.Add(item);
        }
    }

    private void Grid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
    {
        if (DataContext is CsvDocumentViewModel viewModel && Grid.CurrentItem is BbxEditor.Domain.CsvRow row &&
            !ReferenceEquals(viewModel.SelectedRow, row))
            viewModel.SelectedRow = row;
    }

    internal void CommitEditingAndClearFocus()
    {
        Grid.CommitEdit(DataGridEditingUnit.Cell, true);
        Grid.CommitEdit(DataGridEditingUnit.Row, true);
        Keyboard.ClearFocus();
    }

    private void BuildColumns(CsvDocumentViewModel viewModel)
    {
        Grid.Columns.Clear();
        var descriptions = CsvDocumentCodec.GetFieldDescriptions(viewModel.Csv);
        for (var index = 0; index < viewModel.Csv.Columns.Count; index++)
        {
            var name = viewModel.Csv.Columns[index];
            var description = index < descriptions.Count ? descriptions[index] : string.Empty;
            var metadata = viewModel.Csv.Metadata?.Columns.FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase));
            var columnIndex = index;
            var header = new TextBox
            {
                Text = name,
                ToolTip = metadata is null
                    ? BuildToolTip("Unknown", description, "No exported metadata for this column.", null)
                    : BuildToolTip(metadata.Type.Kind.ToString(), description, metadata.Tooltip, metadata.BindingMemberName),
                Style = (Style)FindResource("CsvColumnHeaderEditorStyle"),
            };
            header.LostKeyboardFocus += (_, _) => CommitHeaderEdit(viewModel, columnIndex, header);
            header.PreviewKeyDown += (_, args) => Header_PreviewKeyDown(args, viewModel, columnIndex, header);
            var turnTo = new MenuItem { Header = "Turn To…" };
            turnTo.Click += (_, _) => OpenTurnTo(viewModel, columnIndex);
            header.ContextMenu = new ContextMenu();
            header.ContextMenu.Items.Add(turnTo);
            var headerContent = new StackPanel { MaxWidth = 240, ContextMenu = header.ContextMenu };
            headerContent.Children.Add(header);
            if (!string.IsNullOrWhiteSpace(description))
            {
                headerContent.Children.Add(new TextBlock
                {
                    Text = description,
                    FontSize = 10,
                    FontWeight = FontWeights.Normal,
                    Foreground = (System.Windows.Media.Brush)FindResource("TextMutedBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    ToolTip = description,
                    Margin = new Thickness(2, 1, 2, 1),
                });
            }
            var binding = new Binding($"Cells[{index}].Value") { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus };
            DataGridColumn column = metadata?.Type.Kind switch
            {
                EditorValueKind.Boolean => new DataGridComboBoxColumn
                {
                    ItemsSource = new[] { "true", "false" },
                    SelectedItemBinding = binding,
                },
                EditorValueKind.Enum when metadata.Type.EnumValues.Count > 0 => new DataGridComboBoxColumn
                {
                    ItemsSource = metadata.Type.EnumValues,
                    SelectedItemBinding = binding,
                },
                _ => new DataGridTextColumn { Binding = binding },
            };
            column.Header = headerContent;
            column.CanUserSort = false;
            column.MinWidth = 90;
            column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            column.IsReadOnly = metadata?.ReadOnly == true;
            Grid.Columns.Add(column);
        }
    }

    private void Header_PreviewKeyDown(KeyEventArgs args, CsvDocumentViewModel viewModel, int columnIndex, TextBox header)
    {
        if (args.Key == Key.Enter)
        {
            CommitHeaderEdit(viewModel, columnIndex, header);
            Grid.Focus();
            args.Handled = true;
        }
        else if (args.Key == Key.Escape)
        {
            header.Text = viewModel.Csv.Columns[columnIndex];
            Grid.Focus();
            args.Handled = true;
        }
    }

    private void CommitHeaderEdit(CsvDocumentViewModel viewModel, int columnIndex, TextBox header)
    {
        if (columnIndex < 0 || columnIndex >= viewModel.Csv.Columns.Count) return;
        var current = viewModel.Csv.Columns[columnIndex];
        var proposed = header.Text.Trim();
        if (proposed.Equals(current, StringComparison.Ordinal)) return;
        if (proposed.Length == 0)
        {
            header.Text = current;
            viewModel.Owner.SetStatus("CSV column names cannot be empty.", true);
            return;
        }
        if (viewModel.Csv.Columns.Where((_, index) => index != columnIndex)
            .Any(name => name.Equals(proposed, StringComparison.OrdinalIgnoreCase)))
        {
            header.Text = current;
            viewModel.Owner.SetStatus($"The CSV already contains a column named '{proposed}'.", true);
            return;
        }

        viewModel.Csv.Columns[columnIndex] = proposed;
        viewModel.MarkDirty();
        viewModel.Owner.SetStatus($"Renamed CSV column '{current}' to '{proposed}'.");
        _ = Dispatcher.BeginInvoke(() => BuildColumns(viewModel), DispatcherPriority.Background);
    }

    private void OpenTurnTo(CsvDocumentViewModel viewModel, int columnIndex)
    {
        if (columnIndex < 0 || columnIndex >= viewModel.Csv.Columns.Count) return;
        Grid.CommitEdit(DataGridEditingUnit.Cell, true);
        Grid.CommitEdit(DataGridEditingUnit.Row, true);
        var columnName = viewModel.Csv.Columns[columnIndex];
        var metadata = viewModel.Csv.Metadata?.Columns.FirstOrDefault(item =>
            string.Equals(item.Name, columnName, StringComparison.OrdinalIgnoreCase));
        var rows = viewModel.Csv.Rows.Select((row, rowIndex) => new CsvColumnSearchResult(
            rowIndex,
            columnIndex < row.Cells.Count ? row.Cells[columnIndex].Value : string.Empty)).ToArray();
        var vectorEnabled = SupportsVectorSearch(metadata);
        CsvValueRanker? vectorRanker = vectorEnabled
            ? (query, values, cancellationToken) => viewModel.Owner.RankCsvColumnValuesAsync(
                BuildCsvColumnVectorKey(viewModel.Csv.FilePath, columnName), query, values, cancellationToken)
            : null;
        var dialog = new CsvColumnSearchWindow(columnName, rows, vectorEnabled, vectorRanker)
        {
            Owner = Window.GetWindow(this),
        };
        if (dialog.ShowDialog() != true || dialog.SelectedResult is not { } selected) return;
        var row = viewModel.Csv.Rows[selected.RowIndex];
        var column = Grid.Columns[columnIndex];
        Grid.SelectedCells.Clear();
        Grid.SelectedItem = row;
        Grid.CurrentCell = new DataGridCellInfo(row, column);
        Grid.SelectedCells.Add(Grid.CurrentCell);
        Grid.ScrollIntoView(row, column);
        Grid.Focus();
    }

    internal static bool SupportsVectorSearch(EditorFieldMetadata? metadata) =>
        metadata?.Type.Kind == EditorValueKind.String;

    internal static string BuildCsvColumnVectorKey(string? filePath, string columnName)
    {
        var csvName = string.IsNullOrWhiteSpace(filePath) ? "New CSV" : Path.GetFileNameWithoutExtension(filePath);
        return $"{csvName}-{columnName}";
    }

    internal static string FormatAssociatedTargetLabel(CsvAssociationTarget target)
    {
        if (string.IsNullOrWhiteSpace(target.TableName)) return "Associated Tables Unavailable";
        var parts = new List<string> { target.TableName };
        if (!string.IsNullOrWhiteSpace(target.Metadata?.TypeName) &&
            !target.Metadata.TypeName.Equals(target.TableName, StringComparison.OrdinalIgnoreCase))
            parts.Add(target.Metadata.TypeName);
        parts.Add(target.File?.ModName ?? "Unavailable");
        return string.Join(" · ", parts);
    }

    internal static string BuildAssociatedTargetToolTip(CsvAssociationTarget target)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(target.Metadata?.FullTypeName)) lines.Add($"Type: {target.Metadata.FullTypeName}");
        if (!string.IsNullOrWhiteSpace(target.Metadata?.DataLoadType)) lines.Add($"Load: {target.Metadata.DataLoadType}");
        if (!string.IsNullOrWhiteSpace(target.File?.RelativePath)) lines.Add($"Path: {target.File.RelativePath}");
        if (!string.IsNullOrWhiteSpace(target.UnavailableReason)) lines.Add($"Unavailable: {target.UnavailableReason}");
        return lines.Count == 0 ? "No target details are available." : string.Join(Environment.NewLine, lines);
    }

    private static string BuildToolTip(string type, string? description, string? tooltip, string? bindingMember)
    {
        var lines = new List<string> { type };
        if (!string.IsNullOrWhiteSpace(description)) lines.Add(description);
        if (!string.IsNullOrWhiteSpace(bindingMember)) lines.Add($"Binding: {bindingMember}");
        if (!string.IsNullOrWhiteSpace(tooltip)) lines.Add(tooltip);
        return string.Join(Environment.NewLine, lines);
    }
}
