#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using BbxCommon;

public class ResourcesDictionaryBuilder
{
    [MenuItem("Tools/Build Resources Dictionary")]
    public static void BuildResourcesDictionary()
    {
        string resourcesPath = "Assets/Resources";
        string outputPath = "Assets/Resources/ResourcesDictionary.json";

        string[] assetPaths = AssetDatabase.FindAssets("", new[] { resourcesPath });
        Array.Sort(assetPaths, (leftGuid, rightGuid) => string.Compare(
            AssetDatabase.GUIDToAssetPath(leftGuid),
            AssetDatabase.GUIDToAssetPath(rightGuid),
            StringComparison.Ordinal));
        Dictionary<string, string> resourceDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        List<string> duplicateResources = new List<string>();

        foreach (string guid in assetPaths)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            if (AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(DefaultAsset)) continue;
            if (assetPath.EndsWith(".meta")) continue;

            if (assetPath.StartsWith(resourcesPath))
            {
                string relativePath = assetPath.Substring(resourcesPath.Length + 1);
                string fileName = Path.GetFileNameWithoutExtension(relativePath);
                relativePath = Path.ChangeExtension(relativePath, null);

                if (resourceDict.TryGetValue(fileName, out var existingPath))
                {
                    duplicateResources.Add($"{fileName}: kept '{existingPath}', ignored '{relativePath}'");
                    continue;
                }

                resourceDict.Add(fileName, relativePath);
            }
        }

        if (duplicateResources.Count > 0)
        {
            Debug.LogWarning(
                $"Resources dictionary found {duplicateResources.Count} duplicate file name(s). " +
                $"The lexicographically first path is used. {string.Join("; ", duplicateResources)}");
        }

        // 使用 JsonApi 序列化并写入文件。
        var jsonData = JsonApi.Serialize(resourceDict);
        File.WriteAllText(outputPath, jsonData.ToJson());
        Debug.Log($"Resources Dictionary built at: {outputPath}");
    }

    [System.Serializable]
    public class ResourceDictionary
    {
        public string name;
        public string path;
    }

    [System.Serializable]
    public class ResourceDictionaryList
    {
        public List<ResourceDictionary> list;
    }
}
#endif
