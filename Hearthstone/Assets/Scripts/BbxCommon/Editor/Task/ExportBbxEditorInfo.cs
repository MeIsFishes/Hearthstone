#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BbxCommon.Internal;
using LitJson;
using UnityEditor;
using UnityEngine;

namespace BbxCommon
{
    internal static class ExportBbxEditorInfo
    {
        private const string ExportPath = "../ExportedBbxEditorInfo";

        internal static void Export(string legacyTaskDirectory)
        {
            var targetDirectory = Path.GetFullPath(ExportPath);
            var stagingDirectory = targetDirectory + ".staging-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(Path.Combine(stagingDirectory, "Task"));
                Directory.CreateDirectory(Path.Combine(stagingDirectory, "Csv"));
                Directory.CreateDirectory(Path.Combine(stagingDirectory, "ScriptableObject"));
                Directory.CreateDirectory(Path.Combine(stagingDirectory, "Assets"));

                foreach (var taskFile in Directory.GetFiles(legacyTaskDirectory, "*.json", SearchOption.TopDirectoryOnly))
                {
                    File.Copy(taskFile, Path.Combine(stagingDirectory, "Task", Path.GetFileName(taskFile)), true);
                }

                var allTypes = ReflectionApi.GetAllTypesEnumerator().Where(type => type != null).ToArray();
                var csvMetadata = ExportCsvMetadata(allTypes, Path.Combine(stagingDirectory, "Csv"));
                var scriptableObjectMetadata = ExportScriptableObjectMetadata(allTypes, Path.Combine(stagingDirectory, "ScriptableObject"));
                var assets = ExportAssetIndex(Path.Combine(stagingDirectory, "Assets", "asset-index.json"));
                WriteJson(new BbxEditorManifest
                {
                    FormatVersion = 3,
                    ExporterVersion = "3.0.0",
                    ProjectName = Application.productName,
                    UnityVersion = Application.unityVersion,
                    GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                    TaskTypeCount = Directory.GetFiles(Path.Combine(stagingDirectory, "Task"), "*.json").Length,
                    CsvTypeCount = csvMetadata.Count,
                    ScriptableObjectTypeCount = scriptableObjectMetadata.Count,
                    AssetCount = assets.Count,
                }, Path.Combine(stagingDirectory, "manifest.json"));

                ReplaceDirectory(stagingDirectory, targetDirectory);
                Debug.Log("Exported BbxEditor metadata to " + targetDirectory + ".");
            }
            catch
            {
                if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, true);
                throw;
            }
        }

        private static List<BbxEditorCsvTypeMetadata> ExportCsvMetadata(IEnumerable<Type> allTypes, string outputDirectory)
        {
            var csvFiles = Directory.GetFiles(Application.dataPath, "*.csv", SearchOption.AllDirectories)
                .GroupBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.OrderBy(path => path, StringComparer.Ordinal).ToArray(), StringComparer.OrdinalIgnoreCase);
            var result = new List<BbxEditorCsvTypeMetadata>();
            foreach (var type in allTypes.Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CsvDataBase))).OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                CsvDataBase instance;
                try
                {
                    instance = Activator.CreateInstance(type) as CsvDataBase;
                }
                catch (Exception exception)
                {
                    Debug.LogError("Could not construct CSV metadata type " + type.FullName + ": " + exception);
                    continue;
                }
                if (instance == null) continue;

                var tableNames = (instance.GetTableNames() ?? Array.Empty<string>()).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var metadata = new BbxEditorCsvTypeMetadata
                {
                    TypeName = type.Name,
                    FullTypeName = type.FullName,
                    DataGroup = instance.GetDataGroup() ?? string.Empty,
                    DataLoadType = instance.GetDataLoadType().ToString(),
                    TableNames = tableNames,
                };
                var members = GetCsvMembers(type);
                var headers = new List<string>();
                foreach (var tableName in tableNames)
                {
                    if (!csvFiles.TryGetValue(tableName, out var paths)) continue;
                    foreach (var path in paths)
                    {
                        foreach (var header in ReadCsvHeader(path))
                        {
                            if (!headers.Contains(header, StringComparer.OrdinalIgnoreCase)) headers.Add(header);
                        }
                    }
                }
                if (headers.Count == 0) headers.AddRange(members.Keys.OrderBy(name => name, StringComparer.Ordinal));

                foreach (var header in headers)
                {
                    var binding = ResolveCsvBinding(header, members);
                    var memberType = binding.MemberType ?? typeof(string);
                    var field = CreateFieldMetadata(header, memberType, binding.MemberInfo, new HashSet<Type>(), 0);
                    ApplyCsvSpecialValueKind(memberType, field.Type);
                    field.BindingMemberName = binding.MemberName ?? string.Empty;
                    metadata.Columns.Add(field);
                }
                result.Add(metadata);
                WriteJson(metadata, Path.Combine(outputDirectory, SafeFileName(type.FullName) + ".json"));
            }
            return result;
        }

        private static List<BbxEditorScriptableObjectTypeMetadata> ExportScriptableObjectMetadata(IEnumerable<Type> allTypes, string outputDirectory)
        {
            var result = new List<BbxEditorScriptableObjectTypeMetadata>();
            foreach (var type in allTypes.Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(BbxScriptableObject))).OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                var scriptGuid = FindMonoScriptGuid(type);
                if (string.IsNullOrEmpty(scriptGuid))
                {
                    Debug.LogError("Could not resolve MonoScript GUID for BbxScriptableObject " + type.FullName + ".");
                    continue;
                }
                var createMenu = type.GetCustomAttribute<CreateAssetMenuAttribute>();
                var metadata = new BbxEditorScriptableObjectTypeMetadata
                {
                    TypeName = type.Name,
                    FullTypeName = type.FullName,
                    BaseTypeName = type.BaseType != null ? type.BaseType.FullName : string.Empty,
                    ScriptGuid = scriptGuid,
                    MenuName = createMenu != null ? createMenu.menuName : string.Empty,
                    DefaultFileName = createMenu != null ? createMenu.fileName : string.Empty,
                    Fields = GetSerializableFields(type, new HashSet<Type>(), 0),
                };
                result.Add(metadata);
                WriteJson(metadata, Path.Combine(outputDirectory, SafeFileName(type.FullName) + ".json"));
            }
            return result;
        }

        private static List<BbxEditorUnityAssetMetadata> ExportAssetIndex(string outputPath)
        {
            var result = new List<BbxEditorUnityAssetMetadata>();
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<BbxScriptableObject>(path);
                if (asset == null) continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var assetGuid, out long fileId);
                result.Add(new BbxEditorUnityAssetMetadata
                {
                    Guid = assetGuid,
                    FileId = fileId,
                    AssetPath = path,
                    TypeName = asset.GetType().FullName,
                    Name = asset.name,
                    LoadingType = asset.LoadingType.ToString(),
                    GroupName = asset.LoadingType == BbxScriptableObject.ELoadingType.GroupedByName ? asset.GroupName ?? string.Empty : "GameEngineDefault",
                });
            }
            result.Sort((left, right) => string.Compare(left.AssetPath, right.AssetPath, StringComparison.Ordinal));
            WriteJson(new BbxEditorAssetIndex { Assets = result }, outputPath);
            return result;
        }

        private static Dictionary<string, BbxEditorCsvMember> GetCsvMembers(Type type)
        {
            var result = new Dictionary<string, BbxEditorCsvMember>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length != 0) continue;
                result[property.Name] = new BbxEditorCsvMember(property.Name, property.PropertyType, property);
            }
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                result[field.Name] = new BbxEditorCsvMember(field.Name, field.FieldType, field);
            }
            return result;
        }

        private static BbxEditorCsvMember ResolveCsvBinding(string columnName, IReadOnlyDictionary<string, BbxEditorCsvMember> members)
        {
            if (members.TryGetValue(columnName, out var exact)) return exact;
            return default;
        }

        private static void ApplyCsvSpecialValueKind(Type type, BbxEditorTypeMetadata metadata)
        {
            if (type == typeof(Color)) metadata.Kind = "Color";
            else if (type == typeof(Vector2)) metadata.Kind = "Vector2";
            else if (type == typeof(Vector3)) metadata.Kind = "Vector3";
            else if (type == typeof(Vector4)) metadata.Kind = "Vector4";
            else if (type == typeof(TaskBlackboardInjection)) metadata.Kind = "TaskBlackboardInjection";
        }

        private static string[] ReadCsvHeader(string path)
        {
            try
            {
                using (var reader = new StreamReader(path, Encoding.UTF8, true))
                {
                    var line = reader.ReadLine();
                    return string.IsNullOrEmpty(line) ? Array.Empty<string>() : line.TrimEnd('\r').Split(',').Select(value => value.Trim()).Where(value => value.Length > 0).ToArray();
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not read CSV header from " + path + ": " + exception);
                return Array.Empty<string>();
            }
        }

        private static List<BbxEditorFieldMetadata> GetSerializableFields(Type type, HashSet<Type> visiting, int depth)
        {
            var result = new List<BbxEditorFieldMetadata>();
            foreach (var field in EnumerateInstanceFields(type))
            {
                if (!IsUnitySerializedField(field)) continue;
                result.Add(CreateFieldMetadata(field.Name, field.FieldType, field, visiting, depth));
            }
            return result;
        }

        private static IEnumerable<FieldInfo> EnumerateInstanceFields(Type type)
        {
            var hierarchy = new Stack<Type>();
            for (var current = type; current != null && current != typeof(ScriptableObject) && current != typeof(UnityEngine.Object); current = current.BaseType)
                hierarchy.Push(current);
            while (hierarchy.Count > 0)
            {
                foreach (var field in hierarchy.Pop().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    yield return field;
            }
        }

        private static bool IsUnitySerializedField(FieldInfo field)
        {
            if (field.IsStatic || field.IsLiteral || field.IsInitOnly || field.IsNotSerialized) return false;
            return field.IsPublic || field.GetCustomAttribute<SerializeField>() != null || field.GetCustomAttribute<SerializeReference>() != null;
        }

        private static BbxEditorFieldMetadata CreateFieldMetadata(string name, Type type, MemberInfo member, HashSet<Type> visiting, int depth)
        {
            var tooltip = member != null ? member.GetCustomAttribute<TooltipAttribute>() : null;
            var range = member != null ? member.GetCustomAttribute<RangeAttribute>() : null;
            var hide = member != null && member.GetCustomAttribute<HideInInspector>() != null;
            var serializeReference = member != null && member.GetCustomAttribute<SerializeReference>() != null;
            var metadata = new BbxEditorFieldMetadata
            {
                Name = name,
                DisplayName = ObjectNames.NicifyVariableName(name),
                Tooltip = tooltip != null ? tooltip.tooltip : string.Empty,
                Type = CreateTypeMetadata(type, visiting, depth),
                ReadOnly = hide || serializeReference,
                Minimum = range != null ? (double?)range.min : null,
                Maximum = range != null ? (double?)range.max : null,
            };
            if (metadata.Type.Kind == "Object" && !metadata.ReadOnly && depth < 8)
                metadata.Fields = GetSerializableFields(type, visiting, depth + 1);
            return metadata;
        }

        private static BbxEditorTypeMetadata CreateTypeMetadata(Type type, HashSet<Type> visiting, int depth)
        {
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) type = nullable;
            var metadata = new BbxEditorTypeMetadata { TypeName = type.FullName ?? type.Name };
            if (type == typeof(string) || type == typeof(char)) metadata.Kind = "String";
            else if (type == typeof(bool)) metadata.Kind = "Boolean";
            else if (type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort) || type == typeof(int)) metadata.Kind = "Int32";
            else if (type == typeof(long)) metadata.Kind = "Int64";
            else if (type == typeof(uint)) metadata.Kind = "UInt32";
            else if (type == typeof(ulong)) metadata.Kind = "UInt64";
            else if (type == typeof(float)) metadata.Kind = "Single";
            else if (type == typeof(double)) metadata.Kind = "Double";
            else if (type == typeof(decimal)) metadata.Kind = "Decimal";
            else if (type.IsEnum)
            {
                metadata.Kind = "Enum";
                foreach (var name in Enum.GetNames(type))
                {
                    metadata.EnumValues.Add(name);
                    metadata.EnumNumericValues[name] = Convert.ToInt64(Enum.Parse(type, name));
                }
            }
            else if (type.IsArray || IsGenericList(type))
            {
                metadata.Kind = "Array";
                metadata.ElementType = CreateTypeMetadata(type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0], visiting, depth + 1);
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(type)) metadata.Kind = "UnityObjectReference";
            else
            {
                metadata.Kind = "Object";
                if (depth >= 8 || !visiting.Add(type)) return metadata;
                visiting.Remove(type);
            }
            return metadata;
        }

        private static bool IsGenericList(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        private static string FindMonoScriptGuid(Type type)
        {
            BbxScriptableObject instance = null;
            try
            {
                instance = ScriptableObject.CreateInstance(type) as BbxScriptableObject;
                var script = instance != null ? MonoScript.FromScriptableObject(instance) : null;
                var path = script != null ? AssetDatabase.GetAssetPath(script) : string.Empty;
                if (!string.IsNullOrEmpty(path)) return AssetDatabase.AssetPathToGUID(path);
            }
            finally
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
            }

            foreach (var guid in AssetDatabase.FindAssets(type.Name + " t:MonoScript"))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(AssetDatabase.GUIDToAssetPath(guid));
                if (script != null && script.GetClass() == type) return guid;
            }
            return string.Empty;
        }

        private static void WriteJson(object value, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var builder = new StringBuilder();
            var writer = new JsonWriter(builder) { PrettyPrint = true, IndentValue = 2 };
            JsonMapper.ToJson(value, writer);
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        private static void ReplaceDirectory(string stagingDirectory, string targetDirectory)
        {
            var backupDirectory = targetDirectory + ".backup-" + Guid.NewGuid().ToString("N");
            var hadTarget = Directory.Exists(targetDirectory);
            try
            {
                if (hadTarget) Directory.Move(targetDirectory, backupDirectory);
                Directory.Move(stagingDirectory, targetDirectory);
                if (Directory.Exists(backupDirectory)) Directory.Delete(backupDirectory, true);
            }
            catch
            {
                if (!Directory.Exists(targetDirectory) && Directory.Exists(backupDirectory)) Directory.Move(backupDirectory, targetDirectory);
                throw;
            }
        }

        private static string SafeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars()) value = value.Replace(invalid, '_');
            return value;
        }

        private struct BbxEditorCsvMember
        {
            internal readonly string MemberName;
            internal readonly Type MemberType;
            internal readonly MemberInfo MemberInfo;

            internal BbxEditorCsvMember(string memberName, Type memberType, MemberInfo memberInfo)
            {
                MemberName = memberName;
                MemberType = memberType;
                MemberInfo = memberInfo;
            }
        }
    }

    internal sealed class BbxEditorManifest
    {
        public int FormatVersion;
        public string ExporterVersion;
        public string ProjectName;
        public string UnityVersion;
        public string GeneratedAtUtc;
        public int TaskTypeCount;
        public int CsvTypeCount;
        public int ScriptableObjectTypeCount;
        public int AssetCount;
    }

    internal sealed class BbxEditorCsvTypeMetadata
    {
        public string TypeName;
        public string FullTypeName;
        public string DataGroup;
        public string DataLoadType;
        public List<string> TableNames = new List<string>();
        public List<BbxEditorFieldMetadata> Columns = new List<BbxEditorFieldMetadata>();
    }

    internal sealed class BbxEditorScriptableObjectTypeMetadata
    {
        public string TypeName;
        public string FullTypeName;
        public string ScriptGuid;
        public string BaseTypeName;
        public string MenuName;
        public string DefaultFileName;
        public List<BbxEditorFieldMetadata> Fields = new List<BbxEditorFieldMetadata>();
    }

    internal sealed class BbxEditorFieldMetadata
    {
        public string Name;
        public string BindingMemberName = string.Empty;
        public string DisplayName;
        public string Tooltip;
        public BbxEditorTypeMetadata Type = new BbxEditorTypeMetadata();
        public bool Required = false;
        public bool Unique = false;
        public bool ReadOnly;
        public double? Minimum;
        public double? Maximum;
        public string ReferenceTypeName = string.Empty;
        public string ReferenceFieldName = string.Empty;
        public List<BbxEditorFieldMetadata> Fields = new List<BbxEditorFieldMetadata>();
    }

    internal sealed class BbxEditorTypeMetadata
    {
        public string Kind = "Unknown";
        public string TypeName;
        public BbxEditorTypeMetadata ElementType;
        public List<string> EnumValues = new List<string>();
        public Dictionary<string, long> EnumNumericValues = new Dictionary<string, long>();
    }

    internal sealed class BbxEditorAssetIndex
    {
        public List<BbxEditorUnityAssetMetadata> Assets = new List<BbxEditorUnityAssetMetadata>();
    }

    internal sealed class BbxEditorUnityAssetMetadata
    {
        public string Guid;
        public long FileId;
        public string AssetPath;
        public string TypeName;
        public string Name;
        public string LoadingType;
        public string GroupName;
    }
}
#endif
