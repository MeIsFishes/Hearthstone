using System.Collections.ObjectModel;
using System.ComponentModel;
using BbxEditor.Contracts;

namespace BbxEditor.Domain;

public sealed class DesignPlanDocument : EditorDocument
{
    public override string Kind => "Design Plan";
    public required string Title { get; init; }
    public string? State { get; init; }
    public string? Priority { get; init; }
    public string? PlanPath { get; init; }
    public string? ReviewPath { get; init; }
    public string? TabTitleOverride { get; init; }
    public required string Markdown { get; init; }
}

public sealed class CsvCell : ObservableObject
{
    private string _value = string.Empty;
    public string Value { get => _value; set => SetProperty(ref _value, value ?? string.Empty); }
}

public sealed class CsvRow : ObservableObject
{
    public ObservableCollection<CsvCell> Cells { get; } = [];
}

public sealed class CsvDocument : EditorDocument
{
    private bool _tracking;

    public override string Kind => "CSV";
    public ObservableCollection<string> Columns { get; } = [];
    public ObservableCollection<string> HeaderComments { get; } = [];
    public ObservableCollection<CsvRow> Rows { get; } = [];
    public CsvTypeMetadata? Metadata { get; set; }
    public string NewLine { get; set; } = Environment.NewLine;
    public bool HasUtf8Bom { get; set; }

    public void AddRow(CsvRow row)
    {
        TrackRow(row);
        Rows.Add(row);
        if (_tracking) IsDirty = true;
    }

    public void RemoveRow(CsvRow row)
    {
        if (Rows.Remove(row) && _tracking) IsDirty = true;
    }

    public void EnableChangeTracking()
    {
        if (_tracking) return;
        _tracking = true;
        foreach (var row in Rows) TrackRow(row);
    }

    private void TrackRow(CsvRow row)
    {
        foreach (var cell in row.Cells)
        {
            cell.PropertyChanged -= CellChanged;
            cell.PropertyChanged += CellChanged;
        }
        row.Cells.CollectionChanged -= CellsChanged;
        row.Cells.CollectionChanged += CellsChanged;
    }

    private void CellsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
    {
        if (args.NewItems is not null)
        {
            foreach (CsvCell cell in args.NewItems) cell.PropertyChanged += CellChanged;
        }
        if (_tracking) IsDirty = true;
    }

    private void CellChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (_tracking && args.PropertyName == nameof(CsvCell.Value)) IsDirty = true;
    }
}

public sealed class ScriptableObjectProperty : ObservableObject
{
    private string _value = string.Empty;
    public required string Path { get; init; }
    public required string DisplayName { get; init; }
    public string? Tooltip { get; init; }
    public EditorTypeMetadata Type { get; init; } = new();
    public bool IsReadOnly { get; init; }
    internal int LineIndex { get; set; }
    internal int LineEndIndex { get; set; }
    internal int ValueStart { get; init; }
    internal bool IsSequence { get; init; }

    public string Value { get => _value; set => SetProperty(ref _value, value ?? string.Empty); }
}

public sealed class ScriptableObjectDocument : EditorDocument
{
    private bool _tracking;
    public override string Kind => "BbxScriptableObject";
    public string ScriptGuid { get; set; } = string.Empty;
    public ScriptableObjectTypeMetadata? Metadata { get; set; }
    public ObservableCollection<ScriptableObjectProperty> Properties { get; } = [];
    internal List<string> SourceLines { get; } = [];
    internal string NewLine { get; set; } = Environment.NewLine;
    internal bool HasUtf8Bom { get; set; }

    public void EnableChangeTracking()
    {
        if (_tracking) return;
        _tracking = true;
        foreach (var property in Properties) property.PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (_tracking && args.PropertyName == nameof(ScriptableObjectProperty.Value)) IsDirty = true;
    }
}
