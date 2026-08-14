using System.Text.Json;
using System.Text.Json.Nodes;
using BbxEditor.Contracts;

namespace BbxEditor.Infrastructure;

public static class LegacyRuntimeWriter
{
    public static string Serialize(RuntimeTaskGroup group)
    {
        var root = FullObject("BbxCommon.TaskGroupInfo");
        root["RootTaskId"] = group.RootTaskId;
        root["BindingContextFullType"] = group.BindingContextFullType;
        var tasks = DictionaryObject(Special("int"), Full("BbxCommon.TaskValueInfo"));
        var dictionaryIndex = 0;
        foreach (var pair in group.Tasks)
        {
            tasks[$"{dictionaryIndex}, Key"] = pair.Key;
            tasks[$"{dictionaryIndex}, Value"] = WriteTask(pair.Value);
            dictionaryIndex++;
        }
        root["TaskInfos"] = tasks;
        return root.ToJsonString(JsonOptions);
    }

    private static JsonObject WriteTask(RuntimeTaskValue task)
    {
        var result = FullObject("BbxCommon.TaskValueInfo");
        result["FullTypeName"] = task.FullTypeName;
        result["FieldInfos"] = ListObject(Full("BbxCommon.TaskFieldInfo"), task.Fields.Select(WriteField));
        result["EnterConditionReferences"] = ListObject(Special("int"), task.EnterConditionReferences.Select(id => JsonValue.Create(id)));
        result["ConditionReferences"] = ListObject(Special("int"), task.ConditionReferences.Select(id => JsonValue.Create(id)));
        result["ExitConditionReferences"] = ListObject(Special("int"), task.ExitConditionReferences.Select(id => JsonValue.Create(id)));
        result["TimelineItemInfos"] = ListObject(Full("BbxCommon.TaskTimelineItemInfo"), task.TimelineItems.Select(WriteTimelineItem));
        return result;
    }

    private static JsonObject WriteField(RuntimeTaskField field)
    {
        var result = FullObject("BbxCommon.TaskFieldInfo");
        result["FieldName"] = field.FieldName;
        var source = FullObject("BbxCommon.ETaskFieldValueSource");
        source["Value"] = field.ValueSource.ToString();
        result["ValueSource"] = source;
        result["Value"] = field.Value;
        return result;
    }

    private static JsonObject WriteTimelineItem(RuntimeTimelineItem item)
    {
        var result = FullObject("BbxCommon.TaskTimelineItemInfo");
        result["StartTime"] = item.StartTime;
        result["Duration"] = item.Duration;
        result["Id"] = item.Id;
        return result;
    }

    private static JsonObject FullObject(string typeName) => new() { [LegacyJson.TypeInfoKey] = Full(typeName) };
    private static JsonObject Full(string typeName) => new() { ["FullType"] = typeName };
    private static JsonObject Special(string typeName) => new() { ["SpecialType"] = typeName };

    private static JsonObject ListObject(JsonObject elementType, IEnumerable<JsonNode?> values)
    {
        var result = new JsonObject
        {
            [LegacyJson.TypeInfoKey] = new JsonObject
            {
                ["SpecialType"] = "List",
                ["GenericType1"] = elementType,
            },
        };
        var index = 0;
        foreach (var value in values)
        {
            result[(index++).ToString()] = value;
        }
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
