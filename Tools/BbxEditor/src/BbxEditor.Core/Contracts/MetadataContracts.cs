using System.Text.Json.Serialization;

namespace BbxEditor.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<EditorValueKind>))]
public enum EditorValueKind
{
    Unknown,
    String,
    Boolean,
    Int32,
    Int64,
    UInt32,
    UInt64,
    Single,
    Double,
    Decimal,
    Color,
    Vector2,
    Vector3,
    Vector4,
    TaskBlackboardInjection,
    Enum,
    Array,
    Object,
    UnityObjectReference,
}

public sealed class EditorTypeMetadata
{
    public EditorValueKind Kind { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public EditorTypeMetadata? ElementType { get; set; }
    public List<string> EnumValues { get; set; } = [];
    public Dictionary<string, long> EnumNumericValues { get; set; } = new(StringComparer.Ordinal);
}

public sealed class EditorFieldMetadata
{
    public string Name { get; set; } = string.Empty;
    public string BindingMemberName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Tooltip { get; set; }
    public EditorTypeMetadata Type { get; set; } = new();
    public bool Required { get; set; }
    public bool Unique { get; set; }
    public bool ReadOnly { get; set; }
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
    public string? ReferenceTypeName { get; set; }
    public string? ReferenceFieldName { get; set; }
    public List<EditorFieldMetadata> Fields { get; set; } = [];
}

public sealed class CsvTypeMetadata
{
    public string TypeName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string DataGroup { get; set; } = string.Empty;
    public string DataLoadType { get; set; } = string.Empty;
    public List<string> TableNames { get; set; } = [];
    public List<EditorFieldMetadata> Columns { get; set; } = [];
}

public sealed class ScriptableObjectTypeMetadata
{
    public string TypeName { get; set; } = string.Empty;
    public string FullTypeName { get; set; } = string.Empty;
    public string ScriptGuid { get; set; } = string.Empty;
    public string BaseTypeName { get; set; } = string.Empty;
    public string MenuName { get; set; } = string.Empty;
    public string DefaultFileName { get; set; } = string.Empty;
    public List<EditorFieldMetadata> Fields { get; set; } = [];
}

public sealed class UnityAssetMetadata
{
    public string Guid { get; set; } = string.Empty;
    public long FileId { get; set; }
    public string AssetPath { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LoadingType { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
}
