using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;


namespace BbxCommon.Internal
{
    public static class ResourceManager
    {
        #region Mod List
        private struct ModInfo
        {
            public string Directory;
            public ModSettings ModSettings;

            public ModInfo(string directory, ModSettings modSettings)
            {
                Directory = directory;
                ModSettings = modSettings;
            }
        }

        /// <summary>
        /// Mods sorted by priority
        /// </summary>
        private static List<ModInfo> m_ModList = new();

        private static void CollectMods()
        {
            try
            {
                if (Directory.Exists("Mods"))
                {
                    foreach (var directory in Directory.EnumerateDirectories(Directory.GetCurrentDirectory() + "/Mods"))
                    {
                        if (File.Exists(directory + "/ModSettings.json") == false)
                            continue;
                        var modSettings = JsonApi.Deserialize<ModSettings>(directory + "/ModSettings.json");
                        if (modSettings == null)
                            continue;
                        // sort mods
                        if (m_ModList.Count == 0)
                        {
                            m_ModList.Add(new ModInfo(directory, modSettings));
                        }
                        else
                        {
                            for (int i = 0; i < m_ModList.Count; i++)
                            {
                                if (m_ModList[i].ModSettings.Priority >= modSettings.Priority)
                                    m_ModList.Insert(i, new ModInfo(directory, modSettings));
                            }
                        }
                    }
                }
                // create "Native" mod if hasn't
                bool nativeCreated = false;
                foreach (var mod in m_ModList)
                {
                    if (mod.ModSettings.Name == "Native")
                    {
                        nativeCreated = true;
                        break;
                    }
                }
                if (nativeCreated == false)
                {
                    var modNative = new ModInfo();
                    modNative.ModSettings = new ModSettings();
                    modNative.ModSettings.Name = "Native";
                    modNative.ModSettings.Enabled = true;
                    modNative.ModSettings.Priority = 1;
                    modNative.ModSettings.Version = 0;
                    m_ModList.Add(modNative);
                }
            }
            catch (Exception e)
            {
                DebugApi.LogError(e);
                return;
            }
        }
        #endregion

        #region File Dic
        // FileDic support storing all files from different mod directories. Its key is file name, and value is all file paths of mod sorted by priority.
        //
        // ******
        // For example, if we have a "CustomMod" which want to take a "SkillData.csv" as addition or override, and "CustomMod" was set more prior than the "Native",
        // data in FileDic may be stored like this:
        //
        // { "GameSettingsData", ["C:/MyGame/Native/GameSettingData.json"] },
        // { "SkillData", ["C:/MyGame/CustomMod/SkillData.csv", "C:/MyGame/Native/SkillData.csv"] },
        //
        // We can see that "CustomMod" has not "GameSettingData", so we get only one of this from "Native".
        // We searched two "SkillData", and the more prior one is in the front of the list.
        // ******

        public enum EFileSource
        {
            Directory,
            Resources,
        }

        public struct FileInfo
        {
            public string Path;
            public EFileSource FileSource;

            public FileInfo(string path, EFileSource fileSource)
            {
                Path = path;
                FileSource = fileSource;
            }
        }

        private static Dictionary<string, List<FileInfo>> m_FileDic = new();

        private static void CollectAllFiles()
        {
            var curDirectories = new HashSet<string>();
            var newDirectories = new HashSet<string>();
            // visit mods by priority
            for (int i = 0; i < m_ModList.Count; i++)
            {
                // add resources files to Native mod
                if (m_ModList[i].ModSettings.Name == "Native")
                {
                    foreach (var pair in m_ResourcesFiles)
                    {
                        if (m_FileDic.ContainsKey(pair.Key) == false)
                            m_FileDic[pair.Key] = new List<FileInfo>();
                        m_FileDic[pair.Key].AddRange(pair.Value);
                    }
                }
                // add other mod files
                var fileList = new List<string>();
                if (Directory.Exists(m_ModList[i].Directory))
                {
                    fileList = FileApi.SearchAllFilesInFolder(m_ModList[i].Directory);
                }
                for (int j = 0; j < fileList.Count; j++)
                {
                    var path = fileList[j];
                    var file = FileApi.GetLastDirectoryOrFileOfPath(path);
                    file = FileApi.RemoveExtensionIfHas(file);
                    if (m_FileDic.ContainsKey(file) == false)
                        m_FileDic.Add(file, new List<FileInfo>());
                    m_FileDic[file].Add(new FileInfo(path, EFileSource.Directory));
                }
            }
        }

        internal static FileInfo GetFirstFile(string key)
        {
            if (m_FileDic.TryGetValue(key, out var list))
                return list[0];
            return new FileInfo(null, EFileSource.Directory);
        }

        internal static List<FileInfo> GetFileList(string key)
        {
            if (m_FileDic.TryGetValue(key, out var list))
                return list;
            return null;
        }
        #endregion

        #region Collect Resrouces
        // Files in Resources will always be added to mod "Native", and more prior than files in "Mods/Native".
        // If there is no "Native" mod, Resources files will be added as the least prior.

        private static Dictionary<string, List<FileInfo>> m_ResourcesFiles = new();

        internal static void CollectResourcesFiles()
        {
#if UNITY_EDITOR
            var resourcesRoot = Application.dataPath + "/Resources";
            var fileList = FileApi.SearchAllFilesInFolder(resourcesRoot);
            for (int i = 0; i < fileList.Count; i++)
            {
                var path = fileList[i];
                path = path.Substring(resourcesRoot.Length + 1);
                var file = FileApi.GetLastDirectoryOrFileOfPath(path);
                var extension = FileApi.GetExtensionIfHas(file);
                if (extension == ".meta")
                    continue;
                file = FileApi.RemoveExtensionIfHas(file);
                path = FileApi.RemoveExtensionIfHas(path);
                if (m_ResourcesFiles.ContainsKey(file) == false)
                    m_ResourcesFiles.Add(file, new List<FileInfo>());
                m_ResourcesFiles[file].Add(new FileInfo(path, EFileSource.Resources));
            }
#else
            InitResourceDictionary();
            foreach (var pair in m_ResourcesPathDic)
            {
                if (m_ResourcesFiles.ContainsKey(pair.Key) == false)
                    m_ResourcesFiles.Add(pair.Key, new List<FileInfo>());
                m_ResourcesFiles[pair.Key].Add(new FileInfo(pair.Value, EFileSource.Resources));
            }
#endif
        }
        #endregion

        #region Body
        private static bool m_HasInitialized;

        internal static void Init()
        {
            if (m_HasInitialized)
                return;
            CollectResourcesFiles();
            CollectMods();
            CollectAllFiles();
            m_HasInitialized = true;
        }

        internal static string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Mod List:\n");
            foreach (var modInfo in m_ModList)
            {
                sb.Append("Directory: ");
                sb.Append(modInfo.Directory);
                sb.Append(", Name: ");
                sb.Append(modInfo.ModSettings.Name);
                sb.Append(", Version: ");
                sb.Append(modInfo.ModSettings.Version);
                sb.Append(", Enabled: ");
                sb.Append(modInfo.ModSettings.Enabled);
                sb.Append(", Priority: ");
                sb.Append(modInfo.ModSettings.Priority);
                sb.Append("\n");
            }
            sb.Append("\nFile Dic:\n");
            foreach (var pair in m_FileDic)
            {
                sb.Append("Key: \n");
                sb.Append(pair.Key);
                sb.Append("\nFiles: \n");
                foreach (var fileInfo in pair.Value)
                {
                    sb.Append(fileInfo.FileSource.ToString());
                    sb.Append("   ");
                    sb.Append(fileInfo.Path);
                    sb.Append("\n");
                }
            }
            return sb.ToString();
        }
        #endregion

        #region Load TextAsset
        internal static TextAsset LoadTextAsset(string key)
        {
            TextAsset res = null;
            if (m_FileDic.TryGetValue(key, out var fileInfos))
            {
                var info = fileInfos[0];
                if (info.FileSource == EFileSource.Directory)
                {
                    var streamReader = new StreamReader(info.Path);
                    res = new(streamReader.ReadToEnd());
                    streamReader.Close();
                }
                else if (info.FileSource == EFileSource.Resources)
                {
                    res = LoadResource<TextAsset>(key);
                    //res = Resources.Load<TextAsset>(info.Path);
                }
            }
            else
            {
                DebugApi.LogWarning("The file you require doesn't exist: " + key);
            }
            return res;
        }

        internal static List<TextAsset> LoadTextAssets(string key)
        {
            var res = new List<TextAsset>();
            if (m_FileDic.TryGetValue(key, out var fileInfo))
            {
                for (int i = 0; i < fileInfo.Count; i++)
                {
                    var info = fileInfo[i];
                    if (info.FileSource == EFileSource.Directory)
                    {
                        var streamReader = new StreamReader(info.Path);
                        res.Add(new(streamReader.ReadToEnd()));
                        streamReader.Close();
                    }
                    else if (info.FileSource == EFileSource.Resources)
                    {
                        //var textAsset = Resources.Load<TextAsset>(info.Path);
                        var textAsset = LoadResource<TextAsset>(key);
                        res.Add(textAsset);
                    }
                }
            }
            else
            {
                DebugApi.LogWarning("The file you require doesn't exist: " + key);
            }
            return res;
        }
        #endregion

        #region Load Sprite
        private static Dictionary<string, Sprite> m_SpriteCache = new();

        internal static Sprite LoadSprite(string key)
        {
            if (m_SpriteCache.TryGetValue(key, out var cached))
                return cached;

            if (m_FileDic.TryGetValue(key, out var fileInfos) == false)
            {
                DebugApi.LogWarning("The file you require doesn't exist: " + key);
                return null;
            }

            var info = fileInfos[0];
            Sprite sprite = null;
            Texture2D texture = null;
            if (info.FileSource == EFileSource.Resources)
            {
                sprite = Resources.Load<Sprite>(info.Path);
                if (sprite == null)
                    texture = Resources.Load<Texture2D>(info.Path);
            }
            else
            {
                var bytes = System.IO.File.ReadAllBytes(info.Path);
                texture = new Texture2D(2, 2);
                if (texture.LoadImage(bytes) == false)
                {
                    UnityEngine.Object.Destroy(texture);
                    DebugApi.LogWarning("The image cannot be loaded as a sprite: " + info.Path);
                    return null;
                }
            }

            if (sprite == null && texture != null)
                sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), new UnityEngine.Vector2(0.5f, 0.5f));

            if (sprite != null)
                m_SpriteCache[key] = sprite;
            return sprite;
        }
        #endregion

        #region Load GameObject
        private static Dictionary<string, GameObject> m_GameObjectCache = new();

        internal static GameObject LoadGameObject(string key)
        {
            if (m_GameObjectCache.TryGetValue(key, out var cached))
                return cached;

            if (m_FileDic.TryGetValue(key, out var fileInfos) == false)
            {
                DebugApi.LogWarning("The file you require doesn't exist: " + key);
                return null;
            }

            for (int i = 0; i < fileInfos.Count; i++)
            {
                var info = fileInfos[i];
                if (info.FileSource != EFileSource.Resources)
                    continue;

                var prefab = Resources.Load<GameObject>(info.Path);
                if (prefab == null)
                    continue;

                m_GameObjectCache[key] = prefab;
                return prefab;
            }

            DebugApi.LogWarning("The prefab cannot be loaded from Resources: " + key);
            return null;
        }
        #endregion

        #region Load Audio
        private static readonly Dictionary<string, AudioClip> m_AudioCache = new();
        private static readonly Dictionary<string, UniTask<AudioClip>> m_AudioLoadTasks = new();

        internal static UniTask<AudioClip> LoadAudio(string key)
        {
            if (string.IsNullOrEmpty(key))
                return UniTask.FromResult<AudioClip>(null);
            if (m_AudioCache.TryGetValue(key, out var cached))
                return UniTask.FromResult(cached);
            if (m_AudioLoadTasks.TryGetValue(key, out var loadingTask))
                return loadingTask;

            var task = LoadAndCacheAudio(key).Preserve();
            m_AudioLoadTasks[key] = task;
            return task;
        }

        private static async UniTask<AudioClip> LoadAndCacheAudio(string key)
        {
            if (!m_FileDic.TryGetValue(key, out var fileInfos))
            {
                DebugApi.LogWarning("The audio file you require doesn't exist: " + key);
                return null;
            }

            for (int i = 0; i < fileInfos.Count; i++)
            {
                var info = fileInfos[i];
                AudioClip clip;
                if (info.FileSource == EFileSource.Resources)
                {
                    clip = Resources.Load<AudioClip>(info.Path);
                    if (clip == null)
                    {
                        DebugApi.LogWarning("The Resources item cannot be loaded as audio: " + info.Path);
                        continue;
                    }
                }
                else
                {
                    clip = await LoadDirectoryAudio(info.Path);
                    if (clip == null)
                        continue;
                }

                m_AudioCache[key] = clip;
                return clip;
            }

            DebugApi.LogWarning("No supported audio source could be loaded for key: " + key);
            return null;
        }

        private static async UniTask<AudioClip> LoadDirectoryAudio(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            if (!TryGetAudioType(path, out var audioType))
            {
                DebugApi.LogWarning("Unsupported Mod audio format: " + path);
                return null;
            }

            try
            {
                var uri = new Uri(Path.GetFullPath(path)).AbsoluteUri;
                using (var request = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
                {
                    await request.SendWebRequest().ToUniTask();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        DebugApi.LogWarning($"Failed to load Mod audio: {path}\n{request.error}");
                        return null;
                    }

                    var clip = DownloadHandlerAudioClip.GetContent(request);
                    if (clip != null)
                        clip.name = Path.GetFileNameWithoutExtension(path);
                    return clip;
                }
            }
            catch (Exception exception)
            {
                DebugApi.LogWarning($"Failed to load Mod audio: {path}\n{exception.Message}");
                return null;
            }
        }

        private static bool TryGetAudioType(string path, out AudioType audioType)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    return true;
                case ".wav":
                    audioType = AudioType.WAV;
                    return true;
                case ".mp3":
                    audioType = AudioType.MPEG;
                    return true;
                case ".aif":
                case ".aiff":
                    audioType = AudioType.AIFF;
                    return true;
                default:
                    audioType = AudioType.UNKNOWN;
                    return false;
            }
        }
        #endregion

        #region Load Csv
        /// <summary>
        /// If find no files, return false, else return true.
        /// </summary>
        internal static bool LoadCsv<T>(string key) where T : CsvDataBase, new()
        {
            if (m_FileDic.TryGetValue(key, out var fileInfos) == false)
                return false;
            var csvObj = new T();
            switch (csvObj.GetDataLoadType())
            {
                case CsvDataBase.EDataLoad.Addition:
                    var textAssets = LoadTextAssets(key);
                    for (int i = 0; i < textAssets.Count; i++)
                    {
                        csvObj.ReadFromString(fileInfos[i].FileSource == EFileSource.Resources ? "Resources/" + fileInfos[i].Path : fileInfos[i].Path,
                            textAssets[i].text);
                    }
                    return true;
                case CsvDataBase.EDataLoad.Override:
                    var textAsset = LoadTextAsset(key);
                    csvObj.ReadFromString(fileInfos[0].FileSource == EFileSource.Resources ? "Resources/" + fileInfos[0].Path : fileInfos[0].Path,
                            textAsset.text);
                    return true;
            }
            return false;
        }

        /// <summary>
        /// If find no files, return false, else return true.
        /// </summary>
        internal static bool LoadCsv(string key, CsvDataBase csvObj)
        {
            if (m_FileDic.TryGetValue(key, out var fileInfos) == false)
                return false;
            switch (csvObj.GetDataLoadType())
            {
                case CsvDataBase.EDataLoad.Addition:
                    var textAssets = LoadTextAssets(key);
                    for (int i = 0; i < textAssets.Count; i++)
                    {
                        csvObj.ReadFromString(fileInfos[i].FileSource == EFileSource.Resources ? "Resources\\" + fileInfos[i].Path : fileInfos[i].Path,
                            textAssets[i].text);
                    }
                    return true;
                case CsvDataBase.EDataLoad.Override:
                    var textAsset = LoadTextAsset(key);
                    csvObj.ReadFromString(fileInfos[0].FileSource == EFileSource.Resources ? "Resources\\" + fileInfos[0].Path : fileInfos[0].Path,
                            textAsset.text);
                    return true;
            }
            return false;
        }
        #endregion

        #region resource dictionary
        private static Dictionary<string, string> m_ResourcesPathDic = new Dictionary<string, string>();
        private static bool m_HasLoadResDictionar = false;

        public static void InitResourceDictionary()
        {
            if (m_HasLoadResDictionar) return;
            m_HasLoadResDictionar = true;

#if UNITY_EDITOR
            ResourcesDictionaryBuilder.BuildResourcesDictionary();
            UnityEditor.AssetDatabase.Refresh();
#endif

            // 加载 ResourcesDictionary.json
            TextAsset dictAsset = Resources.Load<TextAsset>("ResourcesDictionary");
            if (dictAsset == null)
            {
                Debug.LogError("ResourcesDictionary.json not found in Resources!");
                return;
            }

            // 用JsonApi反序列化
            var jsonData = LitJson.JsonMapper.ToObject(dictAsset.text);
            m_ResourcesPathDic = JsonApi.Deserialize<Dictionary<string, string>>(jsonData);
            if (m_ResourcesPathDic == null)
            {
                Debug.LogError("ResourcesDictionary.json format error!");
                return;
            }
        }

        public static T LoadResource<T>(string name) where T : UnityEngine.Object
        {
            InitResourceDictionary();
            if (m_ResourcesPathDic.TryGetValue(name, out var path))
            {
                return Resources.Load<T>(path);
            }
            Debug.LogError($"Resource not found: {name}");
            return null;
        }
        #endregion
    }
}
