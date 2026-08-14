using System.Collections.ObjectModel;

namespace BbxEditor.Contracts;

public static class TaskContractConstants
{
    public const string ListElementSeparator = "%||%";
    public const string TagNormal = "Normal";
    public const string TagAction = "Action";
    public const string TagOnce = "Once";
    public const string TagDuration = "Duration";
    public const string TagCondition = "Condition";
    public const string TagTimeline = "Timeline";
    public const string TagDrive = "Drive";
}

public enum FieldValueSource
{
    Value,
    Context,
    Blackboard,
}

public sealed record TaskTypeReference(
    string TypeName,
    TaskTypeReference? GenericType1 = null,
    TaskTypeReference? GenericType2 = null)
{
    public bool IsConnectPoint => TypeName.StartsWith("TaskConnectPoint", StringComparison.Ordinal);
    public bool IsSingleConnectPoint => TypeName == "TaskConnectPoint.Single";
    public bool IsMultipleConnectPoint => TypeName == "TaskConnectPoint.Multiple";
    public bool IsList => TypeName == "List";
    public bool IsDictionary => TypeName == "Dictionary";
    public bool IsBoolean => TypeName == "bool";

    public override string ToString()
    {
        if (GenericType1 is null)
        {
            return TypeName;
        }

        return GenericType2 is null
            ? $"{TypeName}<{GenericType1}>"
            : $"{TypeName}<{GenericType1}, {GenericType2}>";
    }
}

public sealed record TaskFieldDefinition(string Name, TaskTypeReference Type, string? Comment);

public sealed record TaskDefinition(
    string TypeName,
    string FullTypeName,
    string? Comment,
    IReadOnlyList<string> Tags,
    IReadOnlyList<TaskFieldDefinition> Fields)
{
    public bool HasTag(string tag) => Tags.Contains(tag, StringComparer.Ordinal);
    public string DisplayName => TypeName.StartsWith("TaskNode", StringComparison.Ordinal)
        ? TypeName[8..]
        : TypeName.StartsWith("Task", StringComparison.Ordinal) ? TypeName[4..] : TypeName;
}

public sealed record TaskContextDefinition(string TypeName, IReadOnlyList<TaskFieldDefinition> Fields)
{
    public string ShortTypeName => TypeName[(TypeName.LastIndexOf('.') + 1)..];
    public string DisplayName => ShortTypeName.StartsWith("TaskContext", StringComparison.Ordinal)
        ? ShortTypeName["TaskContext".Length..]
        : ShortTypeName;
}

public sealed record TaskEnumDefinition(string TypeName, IReadOnlyList<string> Values);

public sealed record RuntimeTaskField(string FieldName, FieldValueSource ValueSource, string Value);

public sealed record RuntimeTimelineItem(double StartTime, double Duration, int Id);

public sealed class RuntimeTaskValue
{
    public required string FullTypeName { get; init; }
    public Collection<RuntimeTaskField> Fields { get; } = [];
    public Collection<int> EnterConditionReferences { get; } = [];
    public Collection<int> ConditionReferences { get; } = [];
    public Collection<int> ExitConditionReferences { get; } = [];
    public Collection<RuntimeTimelineItem> TimelineItems { get; } = [];
}

public sealed class RuntimeTaskGroup
{
    public int RootTaskId { get; set; }
    public string BindingContextFullType { get; set; } = string.Empty;
    public SortedDictionary<int, RuntimeTaskValue> Tasks { get; } = [];
}
