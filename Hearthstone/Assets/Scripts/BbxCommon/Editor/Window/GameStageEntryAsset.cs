#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace BbxCommon.Editor
{
    public abstract class GameStageEntryAsset : BbxScriptableObject
    {
        public const string AssetFolder = "Assets/Resources/Editor";
        internal const string EditorDataGroup = "GameStageEditorEntries";

        [SerializeField]
        private string m_DisplayName;

        public string DisplayName => string.IsNullOrWhiteSpace(m_DisplayName) ? name : m_DisplayName;

        public abstract bool ValidateEntry(out string error);

        /// <summary>
        /// Legacy one-shot entry hook. Existing entry assets remain supported through
        /// the default StageGroup build callback.
        /// </summary>
        public virtual void EnterPlayMode()
        {
            throw new NotSupportedException(
                $"GameStage entry '{DisplayName}' must override EnterPlayMode or CreateStageGroupBuildCallback.");
        }

        /// <summary>
        /// Creates the callback that builds or enters this StageGroup in Play Mode.
        /// The Editor launcher invokes it once per update until it returns true.
        /// </summary>
        public virtual Func<bool> CreateStageGroupBuildCallback()
        {
            return InvokeLegacyEnterPlayMode;
        }

        private bool InvokeLegacyEnterPlayMode()
        {
            EnterPlayMode();
            return true;
        }

        internal void PrepareForEditorStorage()
        {
            LoadingType = ELoadingType.GroupedByName;
            GroupName = EditorDataGroup;
        }

        protected sealed override void OnLoad()
        {
        }

        protected sealed override void OnUnload()
        {
        }

        protected virtual void OnValidate()
        {
            PrepareForEditorStorage();
        }
    }

    [InitializeOnLoad]
    public static class GameStageEntryLauncher
    {
        private const string PendingEntryPathKey = "BbxCommon.GameStageWindow.PendingEntryPath";
        private static Func<bool> s_StageGroupBuildCallback;

        static GameStageEntryLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Loads a StageGroup entry asset and starts Play Mode through that entry.
        /// </summary>
        public static void Start(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                throw new ArgumentException("A GameStage entry asset path is required.", nameof(assetPath));

            var entry = AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(assetPath);
            if (entry == null)
                throw new InvalidOperationException($"Could not load a GameStage entry asset at '{assetPath}'.");

            Start(entry);
        }

        /// <summary>
        /// Saves a configured StageGroup entry asset and starts Play Mode through that entry.
        /// </summary>
        public static void Start(GameStageEntryAsset entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("A GameStage entry can only be started while Unity is in Edit Mode.");

            var path = AssetDatabase.GetAssetPath(entry);
            if (!IsEntryAssetPath(path))
            {
                throw new InvalidOperationException(
                    $"GameStage entry assets must be stored under {GameStageEntryAsset.AssetFolder}.");
            }

            if (!entry.ValidateEntry(out var error))
                throw new InvalidOperationException(error);

            EditorUtility.SetDirty(entry);
            AssetDatabase.SaveAssets();
            SessionState.SetString(PendingEntryPathKey, path);
            EditorApplication.isPlaying = true;
        }

        internal static bool IsEntryAssetPath(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   (path.Equals(GameStageEntryAsset.AssetFolder, StringComparison.OrdinalIgnoreCase) ||
                    path.StartsWith(GameStageEntryAsset.AssetFolder + "/", StringComparison.OrdinalIgnoreCase));
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var pendingPath = SessionState.GetString(PendingEntryPathKey, string.Empty);
            if (state == PlayModeStateChange.ExitingEditMode)
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                var path = pendingPath;
                SessionState.EraseString(PendingEntryPathKey);
                if (string.IsNullOrEmpty(path))
                    return;

                var entry = AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(path);
                if (entry == null)
                {
                    Debug.LogError($"Could not load GameStage entry asset at '{path}'.");
                    return;
                }

                try
                {
                    Debug.Log($"Dispatching GameStage entry '{entry.DisplayName}'.");
                    StartStageGroupBuild(entry);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    EditorApplication.isPlaying = false;
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                StopStageGroupBuild();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                StopStageGroupBuild();
                SessionState.EraseString(PendingEntryPathKey);
            }
        }

        private static void StartStageGroupBuild(GameStageEntryAsset entry)
        {
            StopStageGroupBuild();
            s_StageGroupBuildCallback = entry.CreateStageGroupBuildCallback();
            if (s_StageGroupBuildCallback == null)
            {
                throw new InvalidOperationException(
                    $"GameStage entry '{entry.DisplayName}' returned a null StageGroup build callback.");
            }

            EditorApplication.update += UpdateStageGroupBuild;
            UpdateStageGroupBuild();
        }

        private static void UpdateStageGroupBuild()
        {
            if (!EditorApplication.isPlaying || s_StageGroupBuildCallback == null)
            {
                StopStageGroupBuild();
                return;
            }

            try
            {
                if (!s_StageGroupBuildCallback())
                    return;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                StopStageGroupBuild();
                EditorApplication.isPlaying = false;
                return;
            }

            StopStageGroupBuild();
        }

        private static void StopStageGroupBuild()
        {
            EditorApplication.update -= UpdateStageGroupBuild;
            s_StageGroupBuildCallback = null;
        }
    }

#if ODIN_INSPECTOR
    internal static class GameStageEntryInspectorDrawer
    {
        public static void Draw(PropertyTree tree)
        {
            tree.UpdateTree();
            tree.BeginDraw(true);
            foreach (var property in tree.RootProperty.Children)
            {
                if (IsLoadingSettingsProperty(property.Name))
                    continue;

                property.Draw();
            }
            tree.EndDraw();
        }

        private static bool IsLoadingSettingsProperty(string propertyName)
        {
            return propertyName == nameof(BbxScriptableObject.LoadingType) ||
                   propertyName == nameof(BbxScriptableObject.GroupName) ||
                   propertyName.IndexOf("Loading Settings", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    [CustomEditor(typeof(GameStageEntryAsset), true)]
    internal sealed class GameStageEntryAssetInspector : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            GameStageEntryInspectorDrawer.Draw(Tree);
        }
    }
#else
    [CustomEditor(typeof(GameStageEntryAsset), true)]
    internal sealed class GameStageEntryAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", nameof(GameStageEntryAsset.LoadingType), nameof(GameStageEntryAsset.GroupName));
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
#endif
