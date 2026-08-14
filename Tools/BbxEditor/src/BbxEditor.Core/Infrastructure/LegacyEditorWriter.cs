using System.Text.Json;
using System.Text.Json.Nodes;
using BbxEditor.Contracts;
using BbxEditor.Domain;

namespace BbxEditor.Infrastructure;

public static class LegacyEditorWriter
{
    public static string Serialize(TaskDocument document, TaskCatalog catalog, string? filePathOverride = null)
    {
        var root = document switch
        {
            TimelineDocument timeline => WriteTimeline(timeline, filePathOverride),
            BehaviorTreeDocument tree => WriteTree(tree, catalog, filePathOverride),
            _ => throw new NotSupportedException(document.GetType().FullName),
        };
        return root.ToJsonString(JsonOptions);
    }

    private static JsonObject WriteTimeline(TimelineDocument document, string? filePathOverride)
    {
        var root = FullObject("BbxCommon.EditorModel+TimelineSaveTargetData");
        root["TaskDatas"] = ListObject(Full("BbxCommon.TimelineItemEditData"), document.Items.Select(WriteTimelineItem));
        root["m_MaxTime"] = document.Items.Select(item => item.StartTime + item.Duration).DefaultIfEmpty(0).Max(value => Math.Max(0, value));
        root["m_HasNagetiveDuration"] = document.Items.Any(item => item.Duration < 0);
        WriteDocumentBase(root, document, filePathOverride);
        return root;
    }

    private static JsonObject WriteTimelineItem(TimelineItem item)
    {
        var result = FullObject("BbxCommon.TimelineItemEditData");
        result["OnStartTimeChanged"] = "null";
        result["OnDurationChanged"] = "null";
        result["m_StartTime"] = item.StartTime;
        result["m_Duration"] = item.Duration;
        result["ExpandCondition"] = item.ConditionsExpanded;
        result["EnterConditions"] = ListObject(Full("BbxCommon.TaskEditData"), item.EnterConditions.Select(task => WriteTask(task, "BbxCommon.TaskEditData")));
        result["Conditions"] = ListObject(Full("BbxCommon.TaskEditData"), item.Conditions.Select(task => WriteTask(task, "BbxCommon.TaskEditData")));
        result["ExitConditions"] = ListObject(Full("BbxCommon.TaskEditData"), item.ExitConditions.Select(task => WriteTask(task, "BbxCommon.TaskEditData")));
        WriteTaskBase(result, item.Task);
        return result;
    }

    private static JsonObject WriteTree(BehaviorTreeDocument document, TaskCatalog catalog, string? filePathOverride)
    {
        var root = FullObject("BbxCommon.EditorModel+NodeGraphSaveTargetData");
        var lineDictionary = DictionaryObject(Special("string"), Generic("List", Full("BbxCommon.GraphNodeLineEditData")));
        var dictionaryIndex = 0;
        foreach (var group in document.Edges
                     .GroupBy(edge => (edge.SourceNodeId, edge.SourcePort))
                     .OrderBy(group => document.Nodes.IndexOf(document.Nodes.First(node => node.Id == group.Key.SourceNodeId)))
                     .ThenBy(group => GetPortIndex(document, catalog, group.Key.SourceNodeId, group.Key.SourcePort)))
        {
            var source = document.Nodes.First(node => node.Id == group.Key.SourceNodeId);
            var key = $"{source.Name}.{group.Key.SourcePort}";
            lineDictionary[$"{dictionaryIndex}, Key"] = key;
            lineDictionary[$"{dictionaryIndex}, Value"] = ListObject(
                Full("BbxCommon.GraphNodeLineEditData"),
                group.OrderBy(edge => edge.Order).Select(edge => WriteEdge(document, catalog, edge)));
            dictionaryIndex++;
        }
        root["NodeLineEditDataList"] = lineDictionary;

        var nodeDictionary = DictionaryObject(Special("string"), Full("BbxCommon.GraphNodeEditData"));
        for (var index = 0; index < document.Nodes.Count; index++)
        {
            var node = document.Nodes[index];
            nodeDictionary[$"{index}, Key"] = node.Name;
            nodeDictionary[$"{index}, Value"] = WriteNode(node, catalog);
        }
        root["NodeEditDataDictionary"] = nodeDictionary;
        WriteDocumentBase(root, document, filePathOverride);
        return root;
    }

    private static JsonObject WriteEdge(BehaviorTreeDocument document, TaskCatalog catalog, BehaviorEdge edge)
    {
        var source = document.Nodes.First(node => node.Id == edge.SourceNodeId);
        var target = document.Nodes.First(node => node.Id == edge.TargetNodeId);
        var result = FullObject("BbxCommon.GraphNodeLineEditData");
        result["FromTask"] = source.Name;
        result["FromPort"] = GetPortIndex(document, catalog, source.Id, edge.SourcePort);
        result["ToTask"] = target.Name;
        result["FieldName"] = edge.SourcePort;
        result["Index"] = edge.Order;
        result["TaskType"] = "null";
        result["Fields"] = ListObject(Full("BbxCommon.TaskEditField"), []);
        return result;
    }

    private static JsonObject WriteNode(BehaviorNode node, TaskCatalog catalog)
    {
        var result = FullObject("BbxCommon.GraphNodeEditData");
        result["Name"] = node.Name;
        var position = FullObject("Godot.Vector2");
        position["X"] = node.Position.X;
        position["Y"] = node.Position.Y;
        result["Pos"] = position;

        var ports = DictionaryObject(Special("int"), Special("string"));
        var portNames = GetPortNames(node, catalog).ToArray();
        for (var index = 0; index < portNames.Length; index++)
        {
            ports[$"{index}, Key"] = index;
            ports[$"{index}, Value"] = portNames[index];
        }
        result["m_PortIndexToFieldName"] = ports;
        WriteTaskBase(result, node.Task);
        return result;
    }

    private static JsonObject WriteTask(TaskInstance task, string typeName)
    {
        var result = FullObject(typeName);
        WriteTaskBase(result, task);
        return result;
    }

    private static void WriteTaskBase(JsonObject result, TaskInstance task)
    {
        result["TaskType"] = task.TaskType;
        result["Fields"] = ListObject(Full("BbxCommon.TaskEditField"), task.Fields.Select(WriteField));
    }

    private static JsonObject WriteField(TaskFieldValue fieldValue)
    {
        var result = FullObject("BbxCommon.TaskEditField");
        result["FieldName"] = fieldValue.FieldName;
        result["TypeInfo"] = WriteTaskType(fieldValue.Type);
        var source = FullObject("BbxCommon.ETaskFieldValueSource");
        source["Value"] = fieldValue.Source.ToString();
        result["ValueSource"] = source;
        result["Value"] = fieldValue.Value;
        return result;
    }

    private static JsonObject WriteTaskType(TaskTypeReference type)
    {
        var result = FullObject("BbxCommon.Internal.TaskExportTypeInfo");
        result["TypeName"] = type.TypeName;
        result["GenericType1"] = type.GenericType1 is null ? JsonValue.Create("null") : WriteTaskType(type.GenericType1);
        result["GenericType2"] = type.GenericType2 is null ? JsonValue.Create("null") : WriteTaskType(type.GenericType2);
        return result;
    }

    private static int GetPortIndex(BehaviorTreeDocument document, TaskCatalog catalog, Guid sourceNodeId, string port)
    {
        var node = document.Nodes.First(item => item.Id == sourceNodeId);
        return Array.IndexOf(GetPortNames(node, catalog).ToArray(), port);
    }

    private static IEnumerable<string> GetPortNames(BehaviorNode node, TaskCatalog catalog)
    {
        if (catalog.FindTask(node.Task.TaskType)?.HasTag(TaskContractConstants.TagCondition) == true) yield break;
        yield return "EnterCondition";
        yield return "Condition";
        yield return "ExitCondition";
        foreach (var fieldValue in node.Task.Fields.Where(item => item.Type.IsConnectPoint))
        {
            yield return fieldValue.FieldName;
        }
    }

    private static void WriteDocumentBase(JsonObject root, TaskDocument document, string? filePathOverride)
    {
        var basePath = filePathOverride ?? document.FilePath ?? string.Empty;
        if (basePath.EndsWith(".editor.json", StringComparison.OrdinalIgnoreCase)) basePath = basePath[..^".editor.json".Length];
        else if (basePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) basePath = basePath[..^5];
        root["m_FilePath"] = basePath.Replace('\\', '/');
        root["m_BindingContextType"] = document.BindingContextType;
    }

    private static JsonObject FullObject(string typeName) => new() { [LegacyJson.TypeInfoKey] = Full(typeName) };
    private static JsonObject Full(string typeName) => new() { ["FullType"] = typeName };
    private static JsonObject Special(string typeName) => new() { ["SpecialType"] = typeName };
    private static JsonObject Generic(string specialType, JsonObject genericType1) => new()
    {
        ["SpecialType"] = specialType,
        ["GenericType1"] = genericType1,
    };

    private static JsonObject ListObject(JsonObject elementType, IEnumerable<JsonNode?> values)
    {
        var result = new JsonObject { [LegacyJson.TypeInfoKey] = Generic("List", elementType) };
        var index = 0;
        foreach (var value in values) result[(index++).ToString()] = value;
        return result;
    }

    private static JsonObject DictionaryObject(JsonObject keyType, JsonObject valueType) => new()
    {
        [LegacyJson.TypeInfoKey] = new JsonObject
        {
            ["SpecialType"] = "Dictionary",
            ["GenericType1"] = keyType,
            ["GenericType2"] = valueType,
        },
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
}
