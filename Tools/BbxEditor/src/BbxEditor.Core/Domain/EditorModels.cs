using System.Collections.ObjectModel;
using BbxEditor.Contracts;

namespace BbxEditor.Domain;

public sealed class TaskFieldValue : ObservableObject
{
    private FieldValueSource _source;
    private string _value = string.Empty;

    public required string FieldName { get; init; }
    public required TaskTypeReference Type { get; set; }
    public string? Comment { get; set; }

    public FieldValueSource Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value ?? string.Empty);
    }

    public TaskFieldValue Clone() => new()
    {
        FieldName = FieldName,
        Type = Type,
        Comment = Comment,
        Source = Source,
        Value = Value,
    };
}

public sealed class TaskInstance : ObservableObject
{
    private string _taskType = string.Empty;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string TaskType
    {
        get => _taskType;
        set => SetProperty(ref _taskType, value);
    }

    public ObservableCollection<TaskFieldValue> Fields { get; } = [];

    public TaskFieldValue? FindField(string name) => Fields.FirstOrDefault(field => field.FieldName == name);

    public static TaskInstance FromDefinition(TaskDefinition definition)
    {
        var result = new TaskInstance { TaskType = definition.TypeName };
        foreach (var field in definition.Fields)
        {
            result.Fields.Add(new TaskFieldValue
            {
                FieldName = field.Name,
                Type = field.Type,
                Comment = field.Comment,
                Source = FieldValueSource.Value,
                Value = string.Empty,
            });
        }
        return result;
    }
}

public abstract class EditorDocument : ObservableObject
{
    private string? _filePath;
    private bool _isDirty;

    public Guid Id { get; init; } = Guid.NewGuid();
    public abstract string Kind { get; }

    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (SetProperty(ref _filePath, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (SetProperty(ref _isDirty, value))
            {
                RaisePropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string DisplayName => $"{(string.IsNullOrWhiteSpace(FilePath) ? $"New {Kind}" : Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(FilePath)))}{(IsDirty ? " *" : string.Empty)}";
}

public abstract class TaskDocument : EditorDocument
{
    private string _bindingContextType = string.Empty;

    public string BindingContextType
    {
        get => _bindingContextType;
        set
        {
            if (SetProperty(ref _bindingContextType, value ?? string.Empty)) IsDirty = true;
        }
    }
}

public sealed class TimelineDocument : TaskDocument
{
    public override string Kind => "Timeline";
    public ObservableCollection<TimelineItem> Items { get; } = [];
}

public sealed class TimelineItem : ObservableObject
{
    private double _startTime;
    private double _duration;
    private bool _conditionsExpanded;

    public Guid Id { get; init; } = Guid.NewGuid();
    public required TaskInstance Task { get; init; }

    public double StartTime
    {
        get => _startTime;
        set => SetProperty(ref _startTime, value);
    }

    public double Duration
    {
        get => _duration;
        set => SetProperty(ref _duration, value);
    }

    public bool ConditionsExpanded
    {
        get => _conditionsExpanded;
        set => SetProperty(ref _conditionsExpanded, value);
    }

    public ObservableCollection<TaskInstance> EnterConditions { get; } = [];
    public ObservableCollection<TaskInstance> Conditions { get; } = [];
    public ObservableCollection<TaskInstance> ExitConditions { get; } = [];
}

public readonly record struct EditorPoint(double X, double Y);

public sealed class BehaviorTreeDocument : TaskDocument
{
    public override string Kind => "Behavior Tree";
    public ObservableCollection<BehaviorNode> Nodes { get; } = [];
    public ObservableCollection<BehaviorEdge> Edges { get; } = [];
}

public sealed class BehaviorNode : ObservableObject
{
    private string _name = string.Empty;
    private EditorPoint _position;

    public Guid Id { get; init; } = Guid.NewGuid();
    public required TaskInstance Task { get; init; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public EditorPoint Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }
}

public sealed class BehaviorEdge
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid SourceNodeId { get; init; }
    public required string SourcePort { get; init; }
    public required Guid TargetNodeId { get; init; }
    public int Order { get; set; }
}
