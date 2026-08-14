using BbxCommon.Internal;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BbxCommon
{
    public static class ResourceApi
    {
        #region Internal Variables
        internal static Dictionary<string, List<CsvDataBase>> DataGroupCsvPairs = new();
        #endregion

        #region Resource Manager
        /// <summary>
        /// Initializes the resource index once for the current application domain.
        /// Game-engine bootstrap and editor validation share this public lifecycle entry.
        /// </summary>
        public static void Initialize()
        {
            ResourceManager.Init();
        }

        public static ResourceManager.FileInfo GetFile(string key)
        {
            return ResourceManager.GetFirstFile(key);
        }

        public static List<ResourceManager.FileInfo> GetAllFile(string key)
        {
            return ResourceManager.GetFileList(key);
        }

        public static TextAsset LoadTextAsset(string key)
        {
            return ResourceManager.LoadTextAsset(key);
        }

        public static List<TextAsset> LoadTextAssets(string key)
        {
            return ResourceManager.LoadTextAssets(key);
        }

        public static Sprite LoadSprite(string key)
        {
            return ResourceManager.LoadSprite(key);
        }

        public static GameObject LoadGameObject(string key)
        {
            return ResourceManager.LoadGameObject(key);
        }

        /// <summary>
        /// Loads the highest-priority audio resource with the specified key.
        /// Resources AudioClips are loaded directly; supported Mod audio files are decoded asynchronously.
        /// </summary>
        public static UniTask<AudioClip> LoadAudio(string key)
        {
            return ResourceManager.LoadAudio(key);
        }

        /// <summary>
        /// Loads audio asynchronously and reports the result without exposing the async task type to callers.
        /// The callback runs on the same player-loop context used by the resource loader.
        /// </summary>
        public static void LoadAudio(string key, System.Action<AudioClip> onCompleted)
        {
            LoadAudioWithCallback(key, onCompleted).Forget();
        }

        private static async UniTaskVoid LoadAudioWithCallback(string key, System.Action<AudioClip> onCompleted)
        {
            var clip = await ResourceManager.LoadAudio(key);
            onCompleted?.Invoke(clip);
        }

        /// <summary>
        /// Load LocKeyCsvData by CsvName (internal LoadCsv); for LocApi language load callbacks from any assembly.
        /// </summary>
        public static void LoadLocKeyTable(string csvName)
        {
            ResourceManager.LoadCsv<LocKeyCsvData>(csvName);
        }
        #endregion

        #region Editor Operation
        public static class EditorOperation
        {
            /// <summary>
            /// "Assets/Resources/MyFolder/MyFile.txt" returns "MyFolder/Myfile".
            /// </summary>
            public static string RelativePathToResourcesPath(string path)
            {
                path = path.TryRemoveStart("Assets/Resources/");
                var dotIndex = path.LastIndexOf('.');
                if (dotIndex != -1)
                    path = path.Substring(0, dotIndex);
                return path;
            }

#if UNITY_EDITOR
            public static void SetDirtyAndSave(Object obj)
            {
                EditorUtility.SetDirty(obj);
                AssetDatabase.SaveAssetIfDirty(obj);
            }

            public static TAsset LoadOrCreateAsset<TAsset>(string path) where TAsset : ScriptableObject
            {
                CreateDirectory(path);
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset != null)
                    return asset;
                else
                {
                    if (path.EndsWith(".asset") == false)
                        path += ".asset";
                    asset = ScriptableObject.CreateInstance<TAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                    return asset;
                }
            }

            public static TAsset LoadOrCreateAssetInResources<TAsset>(string path) where TAsset : ScriptableObject
            {
                CreateDirectoryInResources(path);
                if (path.EndsWith(".asset") == false)
                    path += ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>("Assets/Resources/" + path);
                if (asset != null)
                    return asset;
                else
                {
                    asset = ScriptableObject.CreateInstance<TAsset>();
                    AssetDatabase.CreateAsset(asset, "Assets/Resources/" + path);
                    return asset;
                }
            }

            public static void CreateDirectory(string path)
            {
                path = Application.dataPath + "/" + path.TryRemoveStart("Assets/");
                FileApi.CreateAbsoluteDirectory(path);
            }

            public static void CreateDirectoryInResources(string path)
            {
                path = Application.dataPath + "/Resources/" + path;
                FileApi.CreateAbsoluteDirectory(path);
            }
#endif
        }
        #endregion
    }
}
