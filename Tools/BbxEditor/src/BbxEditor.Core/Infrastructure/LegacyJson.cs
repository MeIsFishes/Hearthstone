using System.Globalization;
using System.Text.Json;
using BbxEditor.Contracts;

namespace BbxEditor.Infrastructure;

internal static class LegacyJson
{
    public const string TypeInfoKey = "Default.TypeInfo";

    public static string? ReadNullableString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var value = property.ValueKind == JsonValueKind.String ? property.GetString() : property.ToString();
        return value == "null" ? null : value;
    }

    public static string ReadString(JsonElement element, string propertyName, string fallback = "") =>
        ReadNullableString(element, propertyName) ?? fallback;

    public static bool ReadBoolean(JsonElement element, string propertyName, bool fallback = false)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        return property.ValueKind == JsonValueKind.True ||
               property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out var value) && value;
    }

    public static int ReadInt32(JsonElement element, string propertyName, int fallback = 0)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        return property.TryGetInt32(out var value) ? value : fallback;
    }

    public static double ReadDouble(JsonElement element, string propertyName, double fallback = 0)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        if (property.TryGetDouble(out var value))
        {
            return value;
        }

        return double.TryParse(property.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ? value : fallback;
    }

    public static string? ReadFullType(JsonElement element)
    {
        if (!element.TryGetProperty(TypeInfoKey, out var typeInfo))
        {
            return null;
        }

        return ReadNullableString(typeInfo, "FullType");
    }

    public static IReadOnlyList<T> ReadList<T>(JsonElement element, Func<JsonElement, T> converter)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return element.EnumerateObject()
            .Select(property => (Property: property, IsIndex: int.TryParse(property.Name, out var index), Index: index))
            .Where(item => item.IsIndex)
            .OrderBy(item => item.Index)
            .Select(item => converter(item.Property.Value))
            .ToArray();
    }

    public static IReadOnlyList<(JsonElement Key, JsonElement Value)> ReadDictionary(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var result = new List<(int Index, JsonElement Key, JsonElement Value)>();
        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.EndsWith(", Key", StringComparison.Ordinal) ||
                !int.TryParse(property.Name[..^5], out var index) ||
                !element.TryGetProperty($"{index}, Value", out var value))
            {
                continue;
            }

            result.Add((index, property.Value, value));
        }

        return result.OrderBy(item => item.Index).Select(item => (item.Key, item.Value)).ToArray();
    }

    public static TaskTypeReference ReadTaskType(JsonElement element)
    {
        var typeName = ReadString(element, "TypeName");
        TaskTypeReference? generic1 = null;
        TaskTypeReference? generic2 = null;
        if (element.TryGetProperty("GenericType1", out var generic1Element) && generic1Element.ValueKind == JsonValueKind.Object)
        {
            generic1 = ReadTaskType(generic1Element);
        }
        if (element.TryGetProperty("GenericType2", out var generic2Element) && generic2Element.ValueKind == JsonValueKind.Object)
        {
            generic2 = ReadTaskType(generic2Element);
        }
        return new TaskTypeReference(typeName, generic1, generic2);
    }

    public static TaskFieldDefinition ReadFieldDefinition(JsonElement element)
    {
        var type = element.TryGetProperty("TypeInfo", out var typeElement)
            ? ReadTaskType(typeElement)
            : new TaskTypeReference("string");
        return new TaskFieldDefinition(ReadString(element, "FieldName"), type, ReadNullableString(element, "Comment"));
    }
}
