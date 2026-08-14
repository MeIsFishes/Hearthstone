using System.Text.Json;
using BbxEditor.Contracts;
using BbxEditor.Diagnostics;
using BbxEditor.Domain;

namespace BbxEditor.Infrastructure;

public static class LegacyEditorImporter
{
    public static OperationResult<EditorDocument> Import(string filePath)
    {
        var result = new OperationResult<EditorDocument>();
        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(filePath));
            var root = json.RootElement;
            var fullType = LegacyJson.ReadFullType(root);
            EditorDocument document = fullType switch
            {
                string type when type.EndsWith("TimelineSaveTargetData", StringComparison.Ordinal) => ReadTimeline(root),
                string type when type.EndsWith("NodeGraphSaveTargetData", StringComparison.Ordinal) => ReadBehaviorTree(root),
                _ => throw new InvalidDataException($"Unrecognized legacy editor file type: {fullType ?? "<missing>"}"),
            };
            document.FilePath = Path.GetFullPath(filePath);
            document.IsDirty = false;
            result.Value = document;
        }
        catch (Exception exception)
        {
            result.Diagnostics.Add(new Diagnostic(DiagnosticSeverity.Error, "LEGACY_EDITOR_IMPORT_FAILED", exception.Message, filePath));
        }
        return result;
    }

    private static TimelineDocument ReadTimeline(JsonElement root)
    {
        var document = new TimelineDocument
        {
            BindingContextType = LegacyJson.ReadString(root, "m_BindingContextType"),
        };
        if (root.TryGetProperty("TaskDatas", out var itemsElement))
        {
            foreach (var itemElement in LegacyJson.ReadList(itemsElement, element => element.Clone()))
            {
                var item = new TimelineItem
                {
                    Task = ReadTask(itemElement),
                    StartTime = LegacyJson.ReadDouble(itemElement, "m_StartTime"),
                    Duration = LegacyJson.ReadDouble(itemElement, "m_Duration"),
                    ConditionsExpanded = LegacyJson.ReadBoolean(itemElement, "ExpandCondition"),
                };
                ReadTaskList(itemElement, "EnterConditions", item.EnterConditions);
                ReadTaskList(itemElement, "Conditions", item.Conditions);
                ReadTaskList(itemElement, "ExitConditions", item.ExitConditions);
                document.Items.Add(item);
            }
        }
        return document;
    }

    private static BehaviorTreeDocument ReadBehaviorTree(JsonElement root)
    {
        var document = new BehaviorTreeDocument
        {
            BindingContextType = LegacyJson.ReadString(root, "m_BindingContextType"),
        };
        var nodeIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        if (root.TryGetProperty("NodeEditDataDictionary", out var nodeDictionary))
        {
            foreach (var (_, value) in LegacyJson.ReadDictionary(nodeDictionary))
            {
                var name = LegacyJson.ReadString(value, "Name");
                var position = value.TryGetProperty("Pos", out var pos)
                    ? new EditorPoint(LegacyJson.ReadDouble(pos, "X"), LegacyJson.ReadDouble(pos, "Y"))
                    : default;
                var node = new BehaviorNode
                {
                    Name = name,
                    Position = position,
                    Task = ReadTask(value),
                };
                nodeIds[name] = node.Id;
                document.Nodes.Add(node);
            }
        }

        if (root.TryGetProperty("NodeLineEditDataList", out var edgeDictionary))
        {
            foreach (var (key, value) in LegacyJson.ReadDictionary(edgeDictionary))
            {
                var dictionaryKey = key.GetString() ?? string.Empty;
                var fallbackPort = dictionaryKey.Contains('.') ? dictionaryKey[(dictionaryKey.IndexOf('.') + 1)..] : string.Empty;
                foreach (var edgeElement in LegacyJson.ReadList(value, element => element.Clone()))
                {
                    var sourceName = LegacyJson.ReadString(edgeElement, "FromTask");
                    var targetName = LegacyJson.ReadString(edgeElement, "ToTask");
                    if (!nodeIds.TryGetValue(sourceName, out var sourceId) || !nodeIds.TryGetValue(targetName, out var targetId))
                    {
                        continue;
                    }
                    document.Edges.Add(new BehaviorEdge
                    {
                        SourceNodeId = sourceId,
                        SourcePort = LegacyJson.ReadString(edgeElement, "FieldName", fallbackPort),
                        TargetNodeId = targetId,
                        Order = LegacyJson.ReadInt32(edgeElement, "Index"),
                    });
                }
            }
        }
        return document;
    }

    private static TaskInstance ReadTask(JsonElement element)
    {
        var task = new TaskInstance { TaskType = LegacyJson.ReadString(element, "TaskType") };
        if (!element.TryGetProperty("Fields", out var fieldsElement))
        {
            return task;
        }

        foreach (var fieldElement in LegacyJson.ReadList(fieldsElement, item => item.Clone()))
        {
            var type = fieldElement.TryGetProperty("TypeInfo", out var typeElement)
                ? LegacyJson.ReadTaskType(typeElement)
                : new TaskTypeReference("string");
            var sourceText = fieldElement.TryGetProperty("ValueSource", out var sourceElement)
                ? LegacyJson.ReadString(sourceElement, "Value", nameof(FieldValueSource.Value))
                : nameof(FieldValueSource.Value);
            _ = Enum.TryParse<FieldValueSource>(sourceText, out var source);
            task.Fields.Add(new TaskFieldValue
            {
                FieldName = LegacyJson.ReadString(fieldElement, "FieldName"),
                Type = type,
                Source = source,
                Value = LegacyJson.ReadString(fieldElement, "Value"),
            });
        }
        return task;
    }

    private static void ReadTaskList(JsonElement parent, string propertyName, ICollection<TaskInstance> destination)
    {
        if (!parent.TryGetProperty(propertyName, out var element))
        {
            return;
        }
        foreach (var item in LegacyJson.ReadList(element, child => child.Clone()))
        {
            destination.Add(ReadTask(item));
        }
    }
}
