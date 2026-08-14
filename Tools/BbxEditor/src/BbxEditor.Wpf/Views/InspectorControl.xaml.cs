using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using BbxEditor.Contracts;
using BbxEditor.Domain;
using BbxEditor.Infrastructure;

namespace BbxEditor.Wpf.Views;

public partial class InspectorControl : UserControl
{
    public static readonly DependencyProperty TaskProperty = DependencyProperty.Register(
        nameof(Task), typeof(TaskInstance), typeof(InspectorControl), new PropertyMetadata(null, OnInputChanged));
    public static readonly DependencyProperty CatalogProperty = DependencyProperty.Register(
        nameof(Catalog), typeof(TaskCatalog), typeof(InspectorControl), new PropertyMetadata(null, OnInputChanged));
    public static readonly DependencyProperty BindingContextTypeProperty = DependencyProperty.Register(
        nameof(BindingContextType), typeof(string), typeof(InspectorControl), new PropertyMetadata(string.Empty, OnInputChanged));
    public static readonly DependencyProperty TimelineItemProperty = DependencyProperty.Register(
        nameof(TimelineItem), typeof(TimelineItem), typeof(InspectorControl), new PropertyMetadata(null, OnInputChanged));
    public static readonly DependencyProperty CsvDocumentProperty = DependencyProperty.Register(
        nameof(CsvDocument), typeof(CsvDocument), typeof(InspectorControl), new PropertyMetadata(null, OnInputChanged));
    public static readonly DependencyProperty CsvRowProperty = DependencyProperty.Register(
        nameof(CsvRow), typeof(CsvRow), typeof(InspectorControl), new PropertyMetadata(null, OnInputChanged));

    private readonly IReadOnlyList<IInspectorStrategy> _strategies = [new CsvInspectorStrategy(), new TaskInspectorStrategy()];
    private readonly List<Action> _csvCellUnsubscribeActions = [];

    public InspectorControl()
    {
        InitializeComponent();
    }

    public TaskInstance? Task
    {
        get => (TaskInstance?)GetValue(TaskProperty);
        set => SetValue(TaskProperty, value);
    }

    public TaskCatalog? Catalog
    {
        get => (TaskCatalog?)GetValue(CatalogProperty);
        set => SetValue(CatalogProperty, value);
    }

    public string BindingContextType
    {
        get => (string)GetValue(BindingContextTypeProperty);
        set => SetValue(BindingContextTypeProperty, value);
    }

    public TimelineItem? TimelineItem
    {
        get => (TimelineItem?)GetValue(TimelineItemProperty);
        set => SetValue(TimelineItemProperty, value);
    }

    public CsvDocument? CsvDocument
    {
        get => (CsvDocument?)GetValue(CsvDocumentProperty);
        set => SetValue(CsvDocumentProperty, value);
    }

    public CsvRow? CsvRow
    {
        get => (CsvRow?)GetValue(CsvRowProperty);
        set => SetValue(CsvRowProperty, value);
    }

    public event EventHandler? FieldChanged;

    private static void OnInputChanged(DependencyObject sender, DependencyPropertyChangedEventArgs _) =>
        ((InspectorControl)sender).BuildRows();

    private void BuildRows()
    {
        if (FieldsPanel is null) return;
        foreach (var unsubscribe in _csvCellUnsubscribeActions) unsubscribe();
        _csvCellUnsubscribeActions.Clear();
        FieldsPanel.Children.Clear();
        var strategy = _strategies.First(item => item.CanHandle(this));
        strategy.Build(this);
    }

    private void BuildTaskRows()
    {
        InspectorTitle.Text = "Task Inspector";
        SetEmptyState("No Task Selected", "Select a task node in the Timeline or Behavior Tree to edit its fields.", Task is null);
        if (Task is null || Catalog is null) return;

        if (TimelineItem is not null) FieldsPanel.Children.Add(CreateTimelineTimingCard(TimelineItem));
        var definition = Catalog.FindTask(Task.TaskType);
        var taskHeader = new StackPanel();
        taskHeader.Children.Add(new TextBlock
        {
            Text = definition?.DisplayName ?? Task.TaskType,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            ToolTip = definition?.Comment,
        });
        taskHeader.Children.Add(new TextBlock
        {
            Text = Task.TaskType,
            FontSize = 11,
            Foreground = ThemeBrush("TextMutedBrush", Brushes.Gray),
            Margin = new Thickness(0, 3, 0, 0),
        });
        FieldsPanel.Children.Add(new Border
        {
            Background = ThemeBrush("AccentSoftBrush", Brushes.DimGray),
            BorderBrush = ThemeBrush("AccentBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(11),
            Margin = new Thickness(0, 0, 0, 12),
            Child = taskHeader,
        });
        foreach (var field in Task.Fields.Where(item => !item.Type.IsConnectPoint))
        {
            FieldsPanel.Children.Add(CreateFieldRow(field));
        }
    }

    private void BuildCsvRows()
    {
        InspectorTitle.Text = "CSV Inspector";
        SetEmptyState("No CSV Row Selected", "Select a row in the CSV editor to edit its values.", CsvRow is null);
        if (CsvDocument is null || CsvRow is null) return;

        var rowIndex = CsvDocument.Rows.IndexOf(CsvRow);
        var header = new StackPanel();
        header.Children.Add(new TextBlock
        {
            Text = rowIndex >= 0 ? $"Row {rowIndex + 1}" : "CSV Row",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
        });
        header.Children.Add(new TextBlock
        {
            Text = CsvDocument.Metadata?.FullTypeName ?? "Untyped CSV",
            FontSize = 11,
            Foreground = ThemeBrush("TextMutedBrush", Brushes.Gray),
            Margin = new Thickness(0, 3, 0, 0),
        });
        FieldsPanel.Children.Add(new Border
        {
            Background = ThemeBrush("AccentSoftBrush", Brushes.DimGray),
            BorderBrush = ThemeBrush("AccentBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(11),
            Margin = new Thickness(0, 0, 0, 12),
            Child = header,
        });

        var descriptions = CsvDocumentCodec.GetFieldDescriptions(CsvDocument);
        for (var columnIndex = 0; columnIndex < CsvDocument.Columns.Count; columnIndex++)
        {
            var columnName = CsvDocument.Columns[columnIndex];
            var description = columnIndex < descriptions.Count ? descriptions[columnIndex] : string.Empty;
            var metadata = CsvDocument.Metadata?.Columns.FirstOrDefault(column =>
                column.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
            FieldsPanel.Children.Add(CreateCsvFieldRow(columnName, description, columnIndex, metadata));
        }
    }

    private FrameworkElement CreateCsvFieldRow(
        string columnName,
        string description,
        int columnIndex,
        EditorFieldMetadata? metadata)
    {
        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = $"{columnName}  ({FormatCsvType(metadata?.Type)})",
            FontWeight = FontWeights.Medium,
            ToolTip = metadata?.Tooltip,
            TextWrapping = TextWrapping.Wrap,
        });
        if (!string.IsNullOrWhiteSpace(description))
        {
            content.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 11,
                Foreground = ThemeBrush("TextSecondaryBrush", Brushes.Gray),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }
        if (CsvRow is not null && columnIndex < CsvRow.Cells.Count)
        {
            content.Children.Add(CreateCsvValueEditor(CsvRow.Cells[columnIndex], metadata));
        }
        else
        {
            content.Children.Add(new TextBlock
            {
                Text = "This row has no value for the column.",
                Foreground = ThemeBrush("DangerBrush", Brushes.Firebrick),
                TextWrapping = TextWrapping.Wrap,
            });
        }
        if (!string.IsNullOrWhiteSpace(metadata?.Tooltip) &&
            !metadata.Tooltip.Equals(description, StringComparison.OrdinalIgnoreCase))
        {
            content.Children.Add(new TextBlock
            {
                Text = metadata.Tooltip,
                Foreground = ThemeBrush("TextMutedBrush", Brushes.DimGray),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 3, 0, 0),
            });
        }
        return new Border
        {
            Background = ThemeBrush("SurfaceBrush", Brushes.Transparent),
            BorderBrush = ThemeBrush("BorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = content,
        };
    }

    private FrameworkElement CreateCsvValueEditor(CsvCell cell, EditorFieldMetadata? metadata)
    {
        var editorKind = ResolveCsvEditorKind(metadata);
        if (editorKind == CsvInspectorEditorKind.Array)
            return CreateCsvArrayEditor(cell, metadata!);
        if (editorKind == CsvInspectorEditorKind.Vector)
            return CreateCsvVectorEditor(cell, metadata!);
        if (editorKind == CsvInspectorEditorKind.Color)
            return CreateCsvColorEditor(cell, metadata!);
        if (editorKind == CsvInspectorEditorKind.TaskBlackboardInjection)
            return CreateCsvTaskBlackboardInjectionEditor(cell, metadata!);
        if (editorKind is CsvInspectorEditorKind.BooleanOptions or CsvInspectorEditorKind.EnumOptions)
        {
            var options = editorKind == CsvInspectorEditorKind.BooleanOptions
                ? new[] { "true", "false" }
                : metadata!.Type.EnumValues.ToArray();
            var combo = new ComboBox
            {
                ItemsSource = options,
                IsEnabled = metadata?.ReadOnly != true,
                Margin = new Thickness(0, 4, 0, 0),
                ToolTip = metadata?.Tooltip,
            };
            combo.SelectedItem = FindCsvOption(options, cell.Value);
            var syncing = false;
            combo.SelectionChanged += (_, _) =>
            {
                if (!syncing) ApplyCsvValue(cell, combo.SelectedItem?.ToString() ?? string.Empty);
            };
            SubscribeToCsvCell(cell, value =>
            {
                syncing = true;
                try { combo.SelectedItem = FindCsvOption(options, value); }
                finally { syncing = false; }
            });
            return combo;
        }

        var text = new TextBox
        {
            Text = cell.Value,
            IsReadOnly = metadata?.ReadOnly == true,
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 30,
            Margin = new Thickness(0, 4, 0, 0),
            ToolTip = metadata?.Tooltip ?? "Value",
        };
        text.TextChanged += (_, _) => ApplyCsvValue(cell, text.Text);
        SubscribeToCsvCell(cell, value =>
        {
            if (!text.Text.Equals(value, StringComparison.Ordinal)) text.Text = value;
        });
        return text;
    }

    private FrameworkElement CreateCsvVectorEditor(CsvCell cell, EditorFieldMetadata metadata)
    {
        var componentCount = CsvInspectorValueCodec.GetVectorComponentCount(metadata.Type.Kind);
        var components = CsvInspectorValueCodec.DecodeVector(cell.Value, componentCount);
        var editors = new TextBox[componentCount];
        var syncing = false;
        var grid = new UniformGrid
        {
            Columns = componentCount,
            Margin = new Thickness(0, 4, 0, 0),
        };

        for (var index = 0; index < componentCount; index++)
        {
            var captured = index;
            var componentLabel = "XYZW"[index];
            var group = new StackPanel { Margin = new Thickness(index == 0 ? 0 : 3, 0, index + 1 == componentCount ? 0 : 3, 0) };
            group.Children.Add(new TextBlock
            {
                Text = componentLabel.ToString(),
                FontSize = 10,
                Foreground = ThemeBrush("TextSecondaryBrush", Brushes.Gray),
                Margin = new Thickness(2, 0, 0, 2),
            });
            var editor = new TextBox
            {
                Text = components[index],
                IsReadOnly = metadata.ReadOnly,
                Padding = new Thickness(4),
                ToolTip = $"{componentLabel} component",
            };
            editor.TextChanged += (_, _) =>
            {
                if (syncing) return;
                components[captured] = editor.Text;
                ApplyCsvValue(cell, CsvInspectorValueCodec.EncodeVector(components));
            };
            editors[index] = editor;
            group.Children.Add(editor);
            grid.Children.Add(group);
        }

        SubscribeToCsvCell(cell, value =>
        {
            var updated = CsvInspectorValueCodec.DecodeVector(value, componentCount);
            syncing = true;
            try
            {
                for (var index = 0; index < componentCount; index++)
                {
                    components[index] = updated[index];
                    if (!editors[index].Text.Equals(updated[index], StringComparison.Ordinal))
                        editors[index].Text = updated[index];
                }
            }
            finally
            {
                syncing = false;
            }
        });

        return grid;
    }

    private FrameworkElement CreateCsvColorEditor(CsvCell cell, EditorFieldMetadata metadata)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 0) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });

        var text = new TextBox
        {
            Text = cell.Value,
            IsReadOnly = metadata.ReadOnly,
            Padding = new Thickness(4),
            ToolTip = "Color in #RRGGBB or #RRGGBBAA format",
        };
        var palette = new Button
        {
            IsEnabled = !metadata.ReadOnly,
            Margin = new Thickness(6, 0, 0, 0),
            Padding = new Thickness(3),
            ToolTip = "Open color palette",
        };
        Grid.SetColumn(palette, 1);
        grid.Children.Add(text);
        grid.Children.Add(palette);

        void UpdateSwatch(string value)
        {
            palette.Background = CsvInspectorValueCodec.TryParseColor(value, out var color)
                ? new SolidColorBrush(Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue))
                : ThemeBrush("SurfaceRaisedBrush", Brushes.Transparent);
        }

        var syncing = false;
        text.TextChanged += (_, _) =>
        {
            UpdateSwatch(text.Text);
            if (!syncing) ApplyCsvValue(cell, text.Text);
        };
        palette.Click += (_, _) =>
        {
            using var dialog = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                AnyColor = true,
            };
            if (CsvInspectorValueCodec.TryParseColor(text.Text, out var current))
                dialog.Color = System.Drawing.Color.FromArgb(current.Red, current.Green, current.Blue);
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            text.Text = CsvInspectorValueCodec.WithRgb(
                text.Text,
                dialog.Color.R,
                dialog.Color.G,
                dialog.Color.B);
        };
        SubscribeToCsvCell(cell, value =>
        {
            if (text.Text.Equals(value, StringComparison.Ordinal)) return;
            syncing = true;
            try { text.Text = value; }
            finally { syncing = false; }
        });
        UpdateSwatch(text.Text);
        return grid;
    }

    private FrameworkElement CreateCsvTaskBlackboardInjectionEditor(CsvCell cell, EditorFieldMetadata metadata)
    {
        if (!TaskBlackboardInjectionCodec.TryParse(cell.Value, out var parsedValues, out var parseError))
            return CreateInvalidCsvTaskBlackboardInjectionEditor(cell, metadata, parseError);

        var values = parsedValues.ToList();
        var root = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        var rows = new StackPanel();
        var readOnly = metadata.ReadOnly;
        var applying = false;
        var validation = new TextBlock
        {
            Foreground = ThemeBrush("DangerBrush", Brushes.Firebrick),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0),
            Visibility = Visibility.Collapsed,
        };
        root.Children.Add(rows);

        void Apply()
        {
            string encoded;
            try
            {
                encoded = TaskBlackboardInjectionCodec.Serialize(values);
            }
            catch (FormatException exception)
            {
                validation.Text = $"Changes are not written until every entry is valid. {exception.Message}";
                validation.Visibility = Visibility.Visible;
                return;
            }

            validation.Visibility = Visibility.Collapsed;
            applying = true;
            try
            {
                ApplyCsvValue(cell, encoded);
            }
            finally
            {
                applying = false;
            }
        }

        void Refresh()
        {
            rows.Children.Clear();
            for (var index = 0; index < values.Count; index++)
            {
                var captured = index;
                rows.Children.Add(CreateCsvTaskBlackboardInjectionEntryEditor(
                    values[index],
                    index,
                    values.Count,
                    readOnly,
                    key =>
                    {
                        values[captured] = values[captured] with { Key = key };
                        Apply();
                    },
                    type =>
                    {
                        values[captured] = values[captured] with { Type = type };
                        Apply();
                    },
                    value =>
                    {
                        values[captured] = values[captured] with { Value = value };
                        Apply();
                    },
                    () =>
                    {
                        (values[captured - 1], values[captured]) = (values[captured], values[captured - 1]);
                        Refresh();
                        Apply();
                    },
                    () =>
                    {
                        (values[captured + 1], values[captured]) = (values[captured], values[captured + 1]);
                        Refresh();
                        Apply();
                    },
                    () =>
                    {
                        values.RemoveAt(captured);
                        Refresh();
                        Apply();
                    }));
            }
        }

        if (!readOnly)
        {
            var add = new Button
            {
                Content = "+ Add Blackboard Entry",
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(0, 3, 0, 0),
            };
            add.Click += (_, _) =>
            {
                values.Add(new TaskBlackboardInjectionValue(
                    CreateUniqueTaskBlackboardKey(values),
                    TaskBlackboardInjectionValueType.String,
                    string.Empty));
                Refresh();
                Apply();
            };
            root.Children.Add(add);
        }
        root.Children.Add(validation);
        root.Children.Add(CreateCollectionProtocolHint(
            "Each entry is stored as Key,Type,Value. Supported types are bool, int, long, float, double, and string."));

        SubscribeToCsvCell(cell, value =>
        {
            if (applying) return;
            if (!TaskBlackboardInjectionCodec.TryParse(value, out var updated, out _))
            {
                Dispatcher.BeginInvoke(BuildRows);
                return;
            }

            values.Clear();
            values.AddRange(updated);
            validation.Visibility = Visibility.Collapsed;
            Refresh();
        });
        Refresh();
        return root;
    }

    private FrameworkElement CreateCsvTaskBlackboardInjectionEntryEditor(
        TaskBlackboardInjectionValue entry,
        int index,
        int count,
        bool readOnly,
        Action<string> keyChanged,
        Action<TaskBlackboardInjectionValueType> typeChanged,
        Action<string> valueChanged,
        Action moveUp,
        Action moveDown,
        Action remove)
    {
        var content = new StackPanel();
        var header = readOnly ? new Grid() : CreateCollectionRow();
        if (readOnly) header.ColumnDefinitions.Add(new ColumnDefinition());
        header.Children.Add(new TextBlock
        {
            Text = $"ENTRY {index + 1}",
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Foreground = ThemeBrush("TextSecondaryBrush", Brushes.Gray),
            VerticalAlignment = VerticalAlignment.Center,
        });
        if (!readOnly) AddMoveButtons(header, index, count, moveUp, moveDown, remove);
        content.Children.Add(header);

        var keyEditor = new TextBox
        {
            Text = entry.Key,
            IsReadOnly = readOnly,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 1, 0, 4),
            ToolTip = "Blackboard key",
        };
        keyEditor.TextChanged += (_, _) => keyChanged(keyEditor.Text);
        content.Children.Add(CreateLabeledCsvEditor("KEY", keyEditor));

        var typeEditor = new ComboBox
        {
            ItemsSource = Enum.GetValues<TaskBlackboardInjectionValueType>(),
            SelectedItem = entry.Type,
            IsEnabled = !readOnly,
            Margin = new Thickness(0, 1, 6, 0),
            ToolTip = "Blackboard value type",
        };
        typeEditor.SelectionChanged += (_, _) =>
        {
            if (typeEditor.SelectedItem is TaskBlackboardInjectionValueType selected)
                typeChanged(selected);
        };
        var valueEditor = new TextBox
        {
            Text = entry.Value,
            IsReadOnly = readOnly,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 1, 0, 0),
            ToolTip = "Blackboard value",
        };
        valueEditor.TextChanged += (_, _) => valueChanged(valueEditor.Text);

        var valueGrid = new Grid();
        valueGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        valueGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var typeGroup = CreateLabeledCsvEditor("TYPE", typeEditor);
        var valueGroup = CreateLabeledCsvEditor("VALUE", valueEditor);
        Grid.SetColumn(valueGroup, 1);
        valueGrid.Children.Add(typeGroup);
        valueGrid.Children.Add(valueGroup);
        content.Children.Add(valueGrid);

        return new Border
        {
            Background = ThemeBrush("SurfaceRaisedBrush", Brushes.Transparent),
            BorderBrush = ThemeBrush("BorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(6),
            Margin = new Thickness(0, 0, 0, 5),
            Child = content,
        };
    }

    private FrameworkElement CreateInvalidCsvTaskBlackboardInjectionEditor(
        CsvCell cell,
        EditorFieldMetadata metadata,
        string initialError)
    {
        var root = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        var raw = new TextBox
        {
            Text = cell.Value,
            IsReadOnly = metadata.ReadOnly,
            AcceptsReturn = false,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 30,
            ToolTip = "Raw TaskBlackboardInjection value",
        };
        var message = new TextBlock
        {
            Text = $"Cannot open the structured editor. {initialError}",
            Foreground = ThemeBrush("DangerBrush", Brushes.Firebrick),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0),
        };
        var repair = new Button
        {
            Content = "Open Structured Editor",
            HorizontalAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(7, 2, 7, 2),
            Margin = new Thickness(0, 4, 0, 0),
            Visibility = metadata.ReadOnly ? Visibility.Collapsed : Visibility.Visible,
        };

        void UpdateState()
        {
            var valid = TaskBlackboardInjectionCodec.TryParse(raw.Text, out _, out var error);
            repair.IsEnabled = valid && !metadata.ReadOnly;
            message.Text = valid
                ? "The raw value is valid. Open the structured editor to continue."
                : $"Cannot open the structured editor. {error}";
            message.Foreground = valid
                ? ThemeBrush("TextMutedBrush", Brushes.DimGray)
                : ThemeBrush("DangerBrush", Brushes.Firebrick);
        }

        var syncing = false;
        raw.TextChanged += (_, _) =>
        {
            UpdateState();
            if (!syncing) ApplyCsvValue(cell, raw.Text);
        };
        repair.Click += (_, _) =>
        {
            if (!TaskBlackboardInjectionCodec.TryParse(raw.Text, out var values, out _)) return;
            ApplyCsvValue(cell, TaskBlackboardInjectionCodec.Serialize(values));
            BuildRows();
        };
        SubscribeToCsvCell(cell, value =>
        {
            if (raw.Text.Equals(value, StringComparison.Ordinal)) return;
            syncing = true;
            try { raw.Text = value; }
            finally { syncing = false; }
        });

        root.Children.Add(raw);
        root.Children.Add(message);
        root.Children.Add(repair);
        root.Children.Add(CreateCollectionProtocolHint(
            "Repair the raw Key,Type,Value;... text before reopening the structured editor."));
        UpdateState();
        return root;
    }

    private static StackPanel CreateLabeledCsvEditor(string label, FrameworkElement editor)
    {
        var group = new StackPanel();
        group.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 9,
            Foreground = ThemeBrush("TextMutedBrush", Brushes.Gray),
            Margin = new Thickness(2, 0, 0, 1),
        });
        group.Children.Add(editor);
        return group;
    }

    private static string CreateUniqueTaskBlackboardKey(IEnumerable<TaskBlackboardInjectionValue> values)
    {
        var existing = values.Select(value => value.Key).ToHashSet(StringComparer.Ordinal);
        for (var suffix = 1; ; suffix++)
        {
            var candidate = suffix == 1 ? "Key" : $"Key{suffix}";
            if (!existing.Contains(candidate)) return candidate;
        }
    }

    private FrameworkElement CreateCsvArrayEditor(CsvCell cell, EditorFieldMetadata metadata)
    {
        var values = CsvArrayValueCodec.Decode(cell.Value).ToList();
        var root = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        var rows = new StackPanel();
        var readOnly = metadata.ReadOnly;
        var applying = false;
        root.Children.Add(rows);

        void Apply()
        {
            applying = true;
            try
            {
                ApplyCsvValue(cell, CsvArrayValueCodec.Encode(values));
            }
            finally
            {
                applying = false;
            }
        }

        void Refresh()
        {
            rows.Children.Clear();
            for (var index = 0; index < values.Count; index++)
            {
                var captured = index;
                var row = readOnly ? new Grid() : CreateCollectionRow();
                if (readOnly) row.ColumnDefinitions.Add(new ColumnDefinition());
                var editor = CreateCsvArrayElementEditor(metadata.Type.ElementType, values[index], readOnly, value =>
                {
                    values[captured] = value;
                    Apply();
                });
                Grid.SetColumn(editor, 0);
                row.Children.Add(editor);
                if (!readOnly)
                {
                    AddMoveButtons(row, captured, values.Count,
                        () => { (values[captured - 1], values[captured]) = (values[captured], values[captured - 1]); Refresh(); Apply(); },
                        () => { (values[captured + 1], values[captured]) = (values[captured], values[captured + 1]); Refresh(); Apply(); },
                        () => { values.RemoveAt(captured); Refresh(); Apply(); });
                }
                rows.Children.Add(row);
            }
        }

        if (!readOnly)
        {
            var add = new Button
            {
                Content = "+ Add Array Item",
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(7, 2, 7, 2),
                Margin = new Thickness(0, 3, 0, 0),
            };
            add.Click += (_, _) =>
            {
                values.Add(CreateCsvArrayDefaultValue(metadata.Type.ElementType));
                Refresh();
                Apply();
            };
            root.Children.Add(add);
        }
        root.Children.Add(CreateCollectionProtocolHint($"Array items are written back to this CSV cell with '{CsvArrayValueCodec.Separator}' separators."));

        SubscribeToCsvCell(cell, value =>
        {
            if (applying) return;
            values.Clear();
            values.AddRange(CsvArrayValueCodec.Decode(value));
            Refresh();
        });
        Refresh();
        return root;
    }

    private FrameworkElement CreateCsvArrayElementEditor(
        EditorTypeMetadata? elementType,
        string value,
        bool readOnly,
        Action<string> changed)
    {
        string[]? options = elementType?.Kind switch
        {
            EditorValueKind.Boolean => ["true", "false"],
            EditorValueKind.Enum when elementType.EnumValues.Count > 0 => elementType.EnumValues.ToArray(),
            _ => null,
        };
        if (options is not null)
        {
            var combo = new ComboBox
            {
                ItemsSource = options,
                SelectedItem = options.FirstOrDefault(option => option.Equals(value, StringComparison.OrdinalIgnoreCase)),
                IsEnabled = !readOnly,
                Margin = new Thickness(0, 1, 3, 1),
            };
            combo.SelectionChanged += (_, _) => changed(combo.SelectedItem?.ToString() ?? string.Empty);
            return combo;
        }

        var text = new TextBox
        {
            Text = value,
            IsReadOnly = readOnly,
            Padding = new Thickness(4),
            Margin = new Thickness(0, 1, 3, 1),
        };
        text.TextChanged += (_, _) => changed(text.Text);
        return text;
    }

    private static string CreateCsvArrayDefaultValue(EditorTypeMetadata? elementType) => elementType?.Kind switch
    {
        EditorValueKind.Boolean => "false",
        EditorValueKind.Enum when elementType.EnumValues.Count > 0 => elementType.EnumValues[0],
        _ => string.Empty,
    };

    private void SubscribeToCsvCell(CsvCell cell, Action<string> changed)
    {
        System.ComponentModel.PropertyChangedEventHandler handler = (_, args) =>
        {
            if (args.PropertyName == nameof(CsvCell.Value)) changed(cell.Value);
        };
        cell.PropertyChanged += handler;
        _csvCellUnsubscribeActions.Add(() => cell.PropertyChanged -= handler);
    }

    private static string? FindCsvOption(IEnumerable<string> options, string value) =>
        options.FirstOrDefault(option => option.Equals(value, StringComparison.OrdinalIgnoreCase));

    private static string FormatCsvType(EditorTypeMetadata? type) => type?.Kind switch
    {
        null => "Unknown",
        EditorValueKind.Array => $"Array<{FormatCsvType(type.ElementType)}>",
        _ => type.Kind.ToString(),
    };

    private void ApplyCsvValue(CsvCell cell, string value)
    {
        if (!TryApplyCsvValue(cell, value)) return;
        FieldChanged?.Invoke(this, EventArgs.Empty);
    }

    internal static bool TryApplyCsvValue(CsvCell cell, string value)
    {
        if (cell.Value.Equals(value, StringComparison.Ordinal)) return false;
        cell.Value = value;
        return true;
    }

    private void SetEmptyState(string title, string description, bool visible)
    {
        EmptyTitle.Text = title;
        EmptyDescription.Text = description;
        EmptyText.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private FrameworkElement CreateTimelineTimingCard(TimelineItem item)
    {
        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = "TIMELINE",
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            Foreground = ThemeBrush("AccentHoverBrush", Brushes.LightGray),
            Margin = new Thickness(0, 0, 0, 8),
        });

        var fields = new Grid();
        fields.ColumnDefinitions.Add(new ColumnDefinition());
        fields.ColumnDefinitions.Add(new ColumnDefinition());
        var start = CreateTimelineNumberField("START TIME", item, nameof(TimelineItem.StartTime));
        var duration = CreateTimelineNumberField("DURATION", item, nameof(TimelineItem.Duration));
        start.Margin = new Thickness(0, 0, 5, 0);
        duration.Margin = new Thickness(5, 0, 0, 0);
        Grid.SetColumn(duration, 1);
        fields.Children.Add(start);
        fields.Children.Add(duration);
        content.Children.Add(fields);

        content.Children.Add(new TextBlock
        {
            Text = "Duration < 0 means infinite duration.",
            FontSize = 10,
            Foreground = ThemeBrush("TextMutedBrush", Brushes.DimGray),
            Margin = new Thickness(0, 7, 0, 0),
        });

        return new Border
        {
            Background = ThemeBrush("AccentSoftBrush", Brushes.DimGray),
            BorderBrush = ThemeBrush("AccentBrush", Brushes.Gray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = content,
        };
    }

    private FrameworkElement CreateTimelineNumberField(string label, TimelineItem item, string propertyName)
    {
        var root = new StackPanel();
        root.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 10,
            Foreground = ThemeBrush("TextSecondaryBrush", Brushes.LightGray),
            Margin = new Thickness(2, 0, 0, 3),
        });
        var editor = new TextBox();
        editor.SetBinding(TextBox.TextProperty, new Binding(propertyName)
        {
            Source = item,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        editor.TextChanged += (_, _) => FieldChanged?.Invoke(this, EventArgs.Empty);
        root.Children.Add(editor);
        return root;
    }

    private FrameworkElement CreateFieldRow(TaskFieldValue field)
    {
        var content = new StackPanel();
        content.Children.Add(new TextBlock
        {
            Text = $"{field.FieldName}  ({field.Type})",
            FontWeight = FontWeights.Medium,
            ToolTip = field.Comment,
            TextWrapping = TextWrapping.Wrap,
        });
        var source = new ComboBox
        {
            ItemsSource = Enum.GetValues<FieldValueSource>(),
            SelectedItem = field.Source,
            Margin = new Thickness(0, 3, 0, 3),
        };
        source.SelectionChanged += (_, _) =>
        {
            if (source.SelectedItem is not FieldValueSource selected || selected == field.Source) return;
            field.Source = selected;
            FieldChanged?.Invoke(this, EventArgs.Empty);
            BuildRows();
        };
        content.Children.Add(source);
        content.Children.Add(CreateValueEditor(field));
        if (!string.IsNullOrWhiteSpace(field.Comment))
        {
            content.Children.Add(new TextBlock
            {
                Text = field.Comment,
                Foreground = ThemeBrush("TextMutedBrush", Brushes.DimGray),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, 0),
            });
        }
        return new Border
        {
            Background = ThemeBrush("SurfaceBrush", Brushes.Transparent),
            BorderBrush = ThemeBrush("BorderBrush", Brushes.DimGray),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 10),
            Child = content,
        };
    }

    private FrameworkElement CreateValueEditor(TaskFieldValue field)
    {
        if (field.Source == FieldValueSource.Context)
        {
            var context = Catalog?.FindContext(BindingContextType);
            var candidates = context?.Fields.Where(candidate => candidate.Type == field.Type).Select(candidate => candidate.Name).ToArray() ?? [];
            return CreateOptionEditor(field, candidates, allowEmpty: true);
        }
        if (field.Source == FieldValueSource.Blackboard)
        {
            return CreateTextEditor(field, "Blackboard key");
        }
        if (field.Type.IsBoolean)
        {
            return CreateOptionEditor(field, ["True", "False"]);
        }
        var enumDefinition = Catalog?.FindEnum(field.Type.TypeName);
        if (enumDefinition is not null)
        {
            return CreateOptionEditor(field, enumDefinition.Values);
        }
        if (field.Type.IsList)
        {
            return CreateListEditor(field);
        }
        if (field.Type.IsDictionary)
        {
            return CreateDictionaryEditor(field);
        }
        if (!UsesTextEditor(field.Type))
        {
            return CreateOptionEditor(field, ["Null"]);
        }
        return CreateTextEditor(field, "Value");
    }

    private FrameworkElement CreateListEditor(TaskFieldValue field)
    {
        if (field.Type.GenericType1 is null || Catalog is null ||
            !TaskValueTypeSupport.IsSupportedScalar(field.Type.GenericType1, Catalog))
        {
            return CreateUnsupportedCollectionText(field.Type);
        }

        var values = LegacyCollectionValueCodec.DecodeList(field.Value).ToList();
        var root = new StackPanel();
        var rows = new StackPanel();
        root.Children.Add(rows);

        void Commit()
        {
            var encoded = LegacyCollectionValueCodec.EncodeList(values);
            if (field.Value == encoded) return;
            field.Value = encoded;
            FieldChanged?.Invoke(this, EventArgs.Empty);
        }

        void Refresh()
        {
            rows.Children.Clear();
            for (var index = 0; index < values.Count; index++)
            {
                var captured = index;
                var row = CreateCollectionRow();
                var editor = CreateScalarEditor(field.Type.GenericType1, values[index], value =>
                {
                    values[captured] = value;
                    Commit();
                });
                Grid.SetColumn(editor, 0);
                row.Children.Add(editor);
                AddMoveButtons(row, captured, values.Count,
                    () => { (values[captured - 1], values[captured]) = (values[captured], values[captured - 1]); Refresh(); Commit(); },
                    () => { (values[captured + 1], values[captured]) = (values[captured], values[captured + 1]); Refresh(); Commit(); },
                    () => { values.RemoveAt(captured); Refresh(); Commit(); });
                rows.Children.Add(row);
            }
        }

        var add = new Button { Content = "+ Add List Item", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(7, 2, 7, 2), Margin = new Thickness(0, 3, 0, 0) };
        add.Click += (_, _) => { values.Add(string.Empty); Refresh(); Commit(); };
        root.Children.Add(add);
        root.Children.Add(CreateCollectionProtocolHint($"Lists are saved as legacy strings separated by {TaskContractConstants.ListElementSeparator}. Nested collections and class objects are not supported."));
        Refresh();
        return root;
    }

    private FrameworkElement CreateDictionaryEditor(TaskFieldValue field)
    {
        if (field.Type.GenericType1 is null || field.Type.GenericType2 is null || Catalog is null ||
            !TaskValueTypeSupport.IsSupportedScalar(field.Type.GenericType1, Catalog) ||
            !TaskValueTypeSupport.IsSupportedScalar(field.Type.GenericType2, Catalog))
        {
            return CreateUnsupportedCollectionText(field.Type);
        }

        var entries = LegacyCollectionValueCodec.DecodeDictionary(field.Value, field.Type.GenericType1, field.Type.GenericType2)
            .Select(pair => new CollectionEntry(pair.Key, pair.Value)).ToList();
        var root = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        var keyHeader = new TextBlock { Text = "Key", Foreground = ThemeBrush("TextMutedBrush", Brushes.DimGray) };
        var valueHeader = new TextBlock { Text = "Value", Foreground = ThemeBrush("TextMutedBrush", Brushes.DimGray) };
        Grid.SetColumn(valueHeader, 1);
        header.Children.Add(keyHeader);
        header.Children.Add(valueHeader);
        root.Children.Add(header);
        var rows = new StackPanel();
        root.Children.Add(rows);

        void Commit()
        {
            var encoded = LegacyCollectionValueCodec.EncodeDictionary(entries.Select(entry =>
                new KeyValuePair<string, string>(entry.Key, entry.Value)), field.Type.GenericType1, field.Type.GenericType2);
            if (field.Value == encoded) return;
            field.Value = encoded;
            FieldChanged?.Invoke(this, EventArgs.Empty);
        }

        void Refresh()
        {
            rows.Children.Clear();
            for (var index = 0; index < entries.Count; index++)
            {
                var captured = index;
                var row = CreateCollectionRow(twoValueColumns: true);
                var keyEditor = CreateScalarEditor(field.Type.GenericType1, entries[index].Key, value => { entries[captured].Key = value; Commit(); });
                var valueEditor = CreateScalarEditor(field.Type.GenericType2, entries[index].Value, value => { entries[captured].Value = value; Commit(); });
                Grid.SetColumn(keyEditor, 0);
                Grid.SetColumn(valueEditor, 1);
                row.Children.Add(keyEditor);
                row.Children.Add(valueEditor);
                AddMoveButtons(row, captured, entries.Count,
                    () => { (entries[captured - 1], entries[captured]) = (entries[captured], entries[captured - 1]); Refresh(); Commit(); },
                    () => { (entries[captured + 1], entries[captured]) = (entries[captured], entries[captured + 1]); Refresh(); Commit(); },
                    () => { entries.RemoveAt(captured); Refresh(); Commit(); });
                rows.Children.Add(row);
            }
        }

        var add = new Button { Content = "+ Add Key-Value Pair", HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(7, 2, 7, 2), Margin = new Thickness(0, 3, 0, 0) };
        add.Click += (_, _) => { entries.Add(new CollectionEntry(string.Empty, string.Empty)); Refresh(); Commit(); };
        root.Children.Add(add);
        root.Children.Add(CreateCollectionProtocolHint("Dictionaries are saved as CrossLibrary JsonApi JSON. Nested collections and class objects are not supported."));
        Refresh();
        return root;
    }

    private FrameworkElement CreateScalarEditor(TaskTypeReference type, string value, Action<string> changed)
    {
        IEnumerable<string>? options = type.IsBoolean ? ["True", "False"] : Catalog?.FindEnum(type.TypeName)?.Values;
        if (options is not null)
        {
            var combo = new ComboBox { ItemsSource = options.ToArray(), SelectedItem = value, Margin = new Thickness(0, 1, 3, 1) };
            combo.SelectionChanged += (_, _) => changed(combo.SelectedItem?.ToString() ?? string.Empty);
            return combo;
        }
        var textBox = new TextBox { Text = value, Padding = new Thickness(4), Margin = new Thickness(0, 1, 3, 1) };
        textBox.TextChanged += (_, _) => changed(textBox.Text);
        return textBox;
    }

    private static Grid CreateCollectionRow(bool twoValueColumns = false)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition());
        if (twoValueColumns) row.ColumnDefinitions.Add(new ColumnDefinition());
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        return row;
    }

    private static void AddMoveButtons(Grid row, int index, int count, Action moveUp, Action moveDown, Action remove)
    {
        var offset = row.ColumnDefinitions.Count - 3;
        var up = CreateSmallButton("↑", moveUp, index > 0);
        var down = CreateSmallButton("↓", moveDown, index + 1 < count);
        var delete = CreateSmallButton("×", remove, true, danger: true);
        Grid.SetColumn(up, offset);
        Grid.SetColumn(down, offset + 1);
        Grid.SetColumn(delete, offset + 2);
        row.Children.Add(up);
        row.Children.Add(down);
        row.Children.Add(delete);
    }

    private static Button CreateSmallButton(string text, Action action, bool enabled, bool danger = false)
    {
        var button = new Button
        {
            Content = text,
            IsEnabled = enabled,
            Margin = new Thickness(1),
            Padding = new Thickness(1),
            Style = ThemeStyle(danger ? "DangerButtonStyle" : "IconButtonStyle"),
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static TextBlock CreateCollectionProtocolHint(string text) => new()
    {
        Text = text,
        Foreground = ThemeBrush("TextMutedBrush", Brushes.DimGray),
        FontSize = 10,
        TextWrapping = TextWrapping.Wrap,
        Margin = new Thickness(0, 2, 0, 0),
    };

    private static TextBlock CreateUnsupportedCollectionText(TaskTypeReference type) => new()
    {
        Text = $"Unsupported collection type: {type}. Elements, keys, and values must be scalar or enum values; nested collections and class objects are not supported.",
        Foreground = ThemeBrush("DangerBrush", Brushes.Firebrick),
        TextWrapping = TextWrapping.Wrap,
    };

    private FrameworkElement CreateTextEditor(TaskFieldValue field, string hint)
    {
        var textBox = new TextBox { Text = field.Value, ToolTip = hint, Padding = new Thickness(4) };
        textBox.TextChanged += (_, _) =>
        {
            if (field.Value == textBox.Text) return;
            field.Value = textBox.Text;
            FieldChanged?.Invoke(this, EventArgs.Empty);
        };
        return textBox;
    }

    private FrameworkElement CreateOptionEditor(TaskFieldValue field, IEnumerable<string> values, bool allowEmpty = false)
    {
        var options = values.ToList();
        if (allowEmpty) options.Insert(0, string.Empty);
        var combo = new ComboBox { ItemsSource = options, SelectedItem = field.Value };
        if (!options.Contains(field.Value)) combo.SelectedIndex = 0;
        combo.SelectionChanged += (_, _) =>
        {
            var value = combo.SelectedItem?.ToString() ?? string.Empty;
            if (field.Value == value) return;
            field.Value = value == "Null" ? string.Empty : value;
            FieldChanged?.Invoke(this, EventArgs.Empty);
        };
        return combo;
    }

    private static bool UsesTextEditor(TaskTypeReference type) => type.TypeName is
        "string" or "int" or "float" or "double" or "long" or "ulong" or "short" or "ushort" or
        "byte" or "sbyte" or "uint" or "decimal" or "char";

    private static Brush ThemeBrush(string key, Brush fallback) =>
        System.Windows.Application.Current.TryFindResource(key) as Brush ?? fallback;

    private static Style? ThemeStyle(string key) =>
        System.Windows.Application.Current.TryFindResource(key) as Style;

    internal static InspectorStrategyKind ResolveStrategyKind(CsvDocument? csvDocument) =>
        csvDocument is null ? InspectorStrategyKind.Task : InspectorStrategyKind.Csv;

    internal static CsvInspectorEditorKind ResolveCsvEditorKind(EditorFieldMetadata? metadata) => metadata?.Type.Kind switch
    {
        EditorValueKind.Array => CsvInspectorEditorKind.Array,
        EditorValueKind.Vector2 or EditorValueKind.Vector3 or EditorValueKind.Vector4 => CsvInspectorEditorKind.Vector,
        EditorValueKind.Color => CsvInspectorEditorKind.Color,
        EditorValueKind.TaskBlackboardInjection => CsvInspectorEditorKind.TaskBlackboardInjection,
        EditorValueKind.Boolean => CsvInspectorEditorKind.BooleanOptions,
        EditorValueKind.Enum when metadata.Type.EnumValues.Count > 0 => CsvInspectorEditorKind.EnumOptions,
        _ => CsvInspectorEditorKind.Text,
    };

    private interface IInspectorStrategy
    {
        bool CanHandle(InspectorControl inspector);
        void Build(InspectorControl inspector);
    }

    private sealed class CsvInspectorStrategy : IInspectorStrategy
    {
        public bool CanHandle(InspectorControl inspector) =>
            ResolveStrategyKind(inspector.CsvDocument) == InspectorStrategyKind.Csv;

        public void Build(InspectorControl inspector) => inspector.BuildCsvRows();
    }

    private sealed class TaskInspectorStrategy : IInspectorStrategy
    {
        public bool CanHandle(InspectorControl inspector) => true;
        public void Build(InspectorControl inspector) => inspector.BuildTaskRows();
    }

    private sealed class CollectionEntry(string key, string value)
    {
        public string Key { get; set; } = key;
        public string Value { get; set; } = value;
    }
}

internal enum InspectorStrategyKind
{
    Task,
    Csv,
}

internal enum CsvInspectorEditorKind
{
    Text,
    Array,
    Vector,
    Color,
    TaskBlackboardInjection,
    BooleanOptions,
    EnumOptions,
}
