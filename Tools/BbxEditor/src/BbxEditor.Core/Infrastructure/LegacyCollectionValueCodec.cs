using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using BbxEditor.Contracts;

namespace BbxEditor.Infrastructure;

public static class LegacyCollectionValueCodec
{
    private const string TypeInfoKey = "Default.TypeInfo";

    public static IReadOnlyList<string> DecodeList(string? value) =>
        string.IsNullOrEmpty(value)
            ? []
            : value.Split(TaskContractConstants.ListElementSeparator, StringSplitOptions.RemoveEmptyEntries);

    public static string EncodeList(IEnumerable<string> values) =>
        string.Concat(values.Select(value => value + TaskContractConstants.ListElementSeparator));

    public static bool IsCrossLibraryDictionaryJson(string? value) =>
        TryDecodeCrossLibraryDictionaryCore(value, null, null, out _, out _);

    public static IReadOnlyList<KeyValuePair<string, string>> DecodeDictionary(string? value)
    {
        if (TryDecodeCrossLibraryDictionaryCore(value, null, null, out var values, out _)) return values;
        return TryDecodeAlternatingDictionary(value, out values) ? values : [];
    }

    public static IReadOnlyList<KeyValuePair<string, string>> DecodeDictionary(
        string? value,
        TaskTypeReference keyType,
        TaskTypeReference valueType)
    {
        if (TryDecodeCrossLibraryDictionary(value, keyType, valueType, out var values, out _)) return values;
        return TryDecodeAlternatingDictionary(value, out values) ? values : [];
    }

    public static bool TryDecodeCrossLibraryDictionary(
        string? value,
        TaskTypeReference keyType,
        TaskTypeReference valueType,
        out IReadOnlyList<KeyValuePair<string, string>> values,
        out string? error) =>
        TryDecodeCrossLibraryDictionaryCore(value, keyType, valueType, out values, out error);

    public static bool TryDecodeAlternatingDictionary(
        string? value,
        out IReadOnlyList<KeyValuePair<string, string>> values)
    {
        values = [];
        if (string.IsNullOrEmpty(value) ||
            !value.Contains(TaskContractConstants.ListElementSeparator, StringComparison.Ordinal) ||
            !value.EndsWith(TaskContractConstants.ListElementSeparator, StringComparison.Ordinal))
        {
            return false;
        }

        var elements = value.Split(TaskContractConstants.ListElementSeparator, StringSplitOptions.None);
        if (elements.Length < 3 || elements[^1].Length != 0) return false;
        var itemCount = elements.Length - 1;
        if (itemCount % 2 != 0 || elements.Take(itemCount).Any(string.IsNullOrEmpty)) return false;

        var result = new List<KeyValuePair<string, string>>(itemCount / 2);
        for (var index = 0; index < itemCount; index += 2)
        {
            result.Add(new KeyValuePair<string, string>(elements[index], elements[index + 1]));
        }
        values = result;
        return true;
    }

    public static string EncodeDictionary(
        IEnumerable<KeyValuePair<string, string>> values,
        TaskTypeReference keyType,
        TaskTypeReference valueType)
    {
        var typeInfo = new JsonObject
        {
            ["SpecialType"] = "Dictionary",
            ["GenericType1"] = WriteTypeInfo(keyType),
            ["GenericType2"] = WriteTypeInfo(valueType),
        };
        var root = new JsonObject { [TypeInfoKey] = typeInfo };
        var index = 0;
        foreach (var pair in values)
        {
            root[$"{index}, Key"] = WriteScalar(pair.Key, keyType);
            root[$"{index}, Value"] = WriteScalar(pair.Value, valueType);
            index++;
        }
        return root.ToJsonString();
    }

    private static bool TryDecodeCrossLibraryDictionaryCore(
        string? value,
        TaskTypeReference? keyType,
        TaskTypeReference? valueType,
        out IReadOnlyList<KeyValuePair<string, string>> values,
        out string? error)
    {
        values = [];
        error = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            error = "The dictionary JSON is empty.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "The root of the dictionary JSON must be an object.";
                return false;
            }
            if (!root.TryGetProperty(TypeInfoKey, out var typeInfo) || typeInfo.ValueKind != JsonValueKind.Object)
            {
                error = $"Object type information is missing: {TypeInfoKey}.";
                return false;
            }
            if (!typeInfo.TryGetProperty("SpecialType", out var specialType) ||
                specialType.ValueKind != JsonValueKind.String || specialType.GetString() != "Dictionary")
            {
                error = "The type information does not describe a Dictionary.";
                return false;
            }
            if (!TryValidateGenericType(typeInfo, "GenericType1", keyType, out error) ||
                !TryValidateGenericType(typeInfo, "GenericType2", valueType, out error))
            {
                return false;
            }

            var indexed = new Dictionary<int, (JsonElement? Key, JsonElement? Value)>();
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals(TypeInfoKey)) continue;
                if (!TryParseEntryProperty(property.Name, out var index, out var isKey))
                {
                    error = $"The dictionary contains an unrecognized property: {property.Name}.";
                    return false;
                }
                indexed.TryGetValue(index, out var pair);
                if (isKey)
                {
                    if (pair.Key.HasValue)
                    {
                        error = $"Dictionary index {index} contains a duplicate Key.";
                        return false;
                    }
                    pair.Key = property.Value;
                }
                else
                {
                    if (pair.Value.HasValue)
                    {
                        error = $"Dictionary index {index} contains a duplicate Value.";
                        return false;
                    }
                    pair.Value = property.Value;
                }
                indexed[index] = pair;
            }

            var result = new List<KeyValuePair<string, string>>(indexed.Count);
            for (var index = 0; index < indexed.Count; index++)
            {
                if (!indexed.TryGetValue(index, out var pair) || !pair.Key.HasValue || !pair.Value.HasValue)
                {
                    error = $"Dictionary entries must be numbered consecutively from 0 and each entry must contain both Key and Value; index {index} is incomplete.";
                    return false;
                }
                if (!TryReadScalar(pair.Key.Value, keyType, out var key, out error) ||
                    !TryReadScalar(pair.Value.Value, valueType, out var itemValue, out error))
                {
                    error = $"Dictionary index {index}: {error}";
                    return false;
                }
                result.Add(new KeyValuePair<string, string>(key, itemValue));
            }
            values = result;
            return true;
        }
        catch (JsonException exception)
        {
            error = $"The JSON could not be parsed: {exception.Message}";
            return false;
        }
        catch (InvalidOperationException exception)
        {
            error = $"The JSON contains an invalid data type: {exception.Message}";
            return false;
        }
    }

    private static bool TryValidateGenericType(
        JsonElement typeInfo,
        string propertyName,
        TaskTypeReference? expected,
        out string? error)
    {
        error = null;
        if (!typeInfo.TryGetProperty(propertyName, out var genericType) || genericType.ValueKind != JsonValueKind.Object)
        {
            error = $"Dictionary type information is missing: {propertyName}.";
            return false;
        }

        if (expected is null)
        {
            if (genericType.TryGetProperty("SpecialType", out var special) && special.ValueKind == JsonValueKind.String ||
                genericType.TryGetProperty("FullType", out var full) && full.ValueKind == JsonValueKind.String)
            {
                return true;
            }
            error = $"Dictionary type information is invalid: {propertyName}.";
            return false;
        }

        var typeProperty = TaskValueTypeSupport.IsBuiltInScalar(expected.TypeName) ? "SpecialType" : "FullType";
        if (!genericType.TryGetProperty(typeProperty, out var actual) ||
            actual.ValueKind != JsonValueKind.String || actual.GetString() != expected.TypeName)
        {
            error = $"Dictionary type information {propertyName} does not match the declared type {expected.TypeName}.";
            return false;
        }
        return true;
    }

    private static bool TryParseEntryProperty(string propertyName, out int index, out bool isKey)
    {
        index = -1;
        isKey = false;
        const string keySuffix = ", Key";
        const string valueSuffix = ", Value";
        string indexText;
        if (propertyName.EndsWith(keySuffix, StringComparison.Ordinal))
        {
            isKey = true;
            indexText = propertyName[..^keySuffix.Length];
        }
        else if (propertyName.EndsWith(valueSuffix, StringComparison.Ordinal))
        {
            indexText = propertyName[..^valueSuffix.Length];
        }
        else
        {
            return false;
        }
        return int.TryParse(indexText, NumberStyles.None, CultureInfo.InvariantCulture, out index) && index >= 0;
    }

    private static JsonObject WriteTypeInfo(TaskTypeReference type) => TaskValueTypeSupport.IsBuiltInScalar(type.TypeName)
        ? new JsonObject { ["SpecialType"] = type.TypeName }
        : new JsonObject { ["FullType"] = type.TypeName };

    private static JsonNode? WriteScalar(string value, TaskTypeReference type)
    {
        if (!TaskValueTypeSupport.IsBuiltInScalar(type.TypeName))
        {
            return new JsonObject
            {
                [TypeInfoKey] = new JsonObject { ["FullType"] = type.TypeName },
                ["Value"] = value,
            };
        }
        return type.TypeName switch
        {
            "bool" when bool.TryParse(value, out var parsed) => JsonValue.Create(parsed),
            "sbyte" when sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => JsonValue.Create(parsed),
            "byte" when byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => JsonValue.Create(parsed),
            "short" when short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => JsonValue.Create(parsed),
            "ushort" when ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => JsonValue.Create(parsed),
            "int" when int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => JsonValue.Create(parsed),
            "uint" when uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => JsonValue.Create(parsed),
            "long" when long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => JsonValue.Create(parsed),
            "ulong" when ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed <= long.MaxValue
                ? JsonValue.Create((long)parsed)
                : JsonValue.Create(parsed.ToString(CultureInfo.InvariantCulture)),
            "float" when float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && float.IsFinite(parsed) => JsonValue.Create(parsed),
            "double" when double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed) => JsonValue.Create(parsed),
            "decimal" when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) =>
                JsonValue.Create(parsed.ToString(CultureInfo.InvariantCulture)),
            _ => JsonValue.Create(value),
        };
    }

    private static bool TryReadScalar(
        JsonElement element,
        TaskTypeReference? expected,
        out string value,
        out string? error)
    {
        value = string.Empty;
        error = null;
        if (expected is not null && !TaskValueTypeSupport.IsBuiltInScalar(expected.TypeName))
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(TypeInfoKey, out var typeInfo) || typeInfo.ValueKind != JsonValueKind.Object ||
                !typeInfo.TryGetProperty("FullType", out var fullType) || fullType.ValueKind != JsonValueKind.String ||
                fullType.GetString() != expected.TypeName ||
                !element.TryGetProperty("Value", out var enumValue) || enumValue.ValueKind != JsonValueKind.String)
            {
                error = $"The enum value does not match the declared type {expected.TypeName}.";
                return false;
            }
            value = enumValue.GetString() ?? string.Empty;
            return true;
        }

        if (expected is null && element.ValueKind == JsonValueKind.Object)
        {
            if (!element.TryGetProperty("Value", out var objectValue) || objectValue.ValueKind != JsonValueKind.String)
            {
                error = "The object value is missing a string Value property.";
                return false;
            }
            value = objectValue.GetString() ?? string.Empty;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            value = element.GetString() ?? string.Empty;
            return true;
        }
        if (element.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            value = element.GetBoolean() ? "True" : "False";
            return true;
        }
        if (element.ValueKind == JsonValueKind.Number)
        {
            value = element.GetRawText();
            return true;
        }
        error = "A scalar must be a string, boolean, number, or an enum object with type information.";
        return false;
    }
}

public static class TaskValueTypeSupport
{
    private static readonly HashSet<string> ScalarTypes = new(StringComparer.Ordinal)
    {
        "bool", "string", "char", "sbyte", "byte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal",
    };

    public static bool IsBuiltInScalar(string typeName) => ScalarTypes.Contains(typeName);

    public static bool IsSupportedScalar(TaskTypeReference? type, TaskCatalog catalog) =>
        type is not null && type.GenericType1 is null && type.GenericType2 is null &&
        (IsBuiltInScalar(type.TypeName) || catalog.FindEnum(type.TypeName) is not null);

    public static bool IsSupportedConstant(TaskTypeReference type, TaskCatalog catalog) =>
        type.IsList && type.GenericType1 is not null && type.GenericType2 is null && IsSupportedScalar(type.GenericType1, catalog) ||
        type.IsDictionary && type.GenericType1 is not null && type.GenericType2 is not null &&
        IsSupportedScalar(type.GenericType1, catalog) && IsSupportedScalar(type.GenericType2, catalog) ||
        IsSupportedScalar(type, catalog);

    public static bool IsValidScalar(string value, TaskTypeReference type, TaskCatalog catalog)
    {
        var enumDefinition = catalog.FindEnum(type.TypeName);
        if (enumDefinition is not null) return enumDefinition.Values.Contains(value, StringComparer.Ordinal);
        return type.TypeName switch
        {
            "string" => true,
            "char" => value.Length == 1,
            "bool" => bool.TryParse(value, out _),
            "sbyte" => sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "byte" => byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "short" => short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "ushort" => ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "int" => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "uint" => uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "long" => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "ulong" => ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
            "float" => float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && float.IsFinite(parsed),
            "double" => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && double.IsFinite(parsed),
            "decimal" => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _),
            _ => false,
        };
    }

    public static bool IsRepresentableDictionaryScalar(string value, TaskTypeReference type, TaskCatalog catalog) =>
        IsValidScalar(value, type, catalog) && !(type.TypeName == "string" && value == "null");

    public static bool TryNormalizeDictionaryKey(
        string value,
        TaskTypeReference type,
        TaskCatalog catalog,
        out string normalized)
    {
        normalized = string.Empty;
        if (!IsRepresentableDictionaryScalar(value, type, catalog)) return false;
        if (catalog.FindEnum(type.TypeName) is not null)
        {
            normalized = $"enum:{type.TypeName}:{value}";
            return true;
        }
        normalized = type.TypeName switch
        {
            "string" => $"string:{value}",
            "char" => $"char:{value}",
            "bool" => $"bool:{bool.Parse(value)}",
            "sbyte" => $"sbyte:{sbyte.Parse(value, CultureInfo.InvariantCulture)}",
            "byte" => $"byte:{byte.Parse(value, CultureInfo.InvariantCulture)}",
            "short" => $"short:{short.Parse(value, CultureInfo.InvariantCulture)}",
            "ushort" => $"ushort:{ushort.Parse(value, CultureInfo.InvariantCulture)}",
            "int" => $"int:{int.Parse(value, CultureInfo.InvariantCulture)}",
            "uint" => $"uint:{uint.Parse(value, CultureInfo.InvariantCulture)}",
            "long" => $"long:{long.Parse(value, CultureInfo.InvariantCulture)}",
            "ulong" => $"ulong:{ulong.Parse(value, CultureInfo.InvariantCulture)}",
            "float" => $"float:{Normalize(float.Parse(value, CultureInfo.InvariantCulture))}",
            "double" => $"double:{Normalize(double.Parse(value, CultureInfo.InvariantCulture))}",
            "decimal" => $"decimal:{decimal.Parse(value, CultureInfo.InvariantCulture).ToString("G29", CultureInfo.InvariantCulture)}",
            _ => value,
        };
        return true;
    }

    private static string Normalize(float value) => value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
    private static string Normalize(double value) => value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);
}
