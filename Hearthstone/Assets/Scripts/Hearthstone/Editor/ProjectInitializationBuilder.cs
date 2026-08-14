#if UNITY_EDITOR
using System;
using BbxCommon;
using BbxCommon.Editor;
using BbxCommon.Ui;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthstone.Editor
{
    /// <summary>
    /// 一次性初始化构建器。由匹配版本的 Unity 执行，确保所有 Unity 资产和 .meta 由 Editor 生成。
    /// </summary>
    [InitializeOnLoad]
    public static class ProjectInitializationBuilder
    {
        private const string AutoBuildSessionKey = "Hearthstone.ProjectInitializationBuilder.HasRun";
        private const string CanvasPrefabPath = "Assets/Resources/Ui/CanvasProto.prefab";
        private const string PlaceholderViewPrefabPath = "Assets/Resources/Ui/PlaceholderView.prefab";
        private const string BootstrapPrefabPath = "Assets/Resources/Bootstrap.prefab";
        private const string UiScenePath = "Assets/Scenes/Ui/Placeholder.unity";
        private const string UiSceneAssetPath = "Assets/Resources/Ui/Placeholder.asset";
        private const string MainScenePath = "Assets/Scenes/Main.unity";
        private const string EntryAssetPath = "Assets/Resources/Editor/InitialStageEntry.asset";
        private const string ScriptableObjectAssetsPath = "Assets/Resources/BbxCommon/ScriptableObjectAssets.asset";

        static ProjectInitializationBuilder()
        {
            if (SessionState.GetBool(AutoBuildSessionKey, false) ||
                AssetDatabase.LoadMainAssetAtPath(MainScenePath) != null)
            {
                return;
            }

            EditorApplication.delayCall += BuildAutomatically;
        }

        private static void BuildAutomatically()
        {
            EditorApplication.delayCall -= BuildAutomatically;
            SessionState.SetBool(AutoBuildSessionKey, true);
            try
            {
                Build();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void Build()
        {
            EnsureCurrentSceneCanBeReplaced();
            EnsureCleanTargets();
            EnsureAssetFolder("Assets/Resources");
            EnsureAssetFolder("Assets/Resources/Ui");
            EnsureAssetFolder("Assets/Scenes");
            EnsureAssetFolder("Assets/Scenes/Ui");

            var canvasPrefab = CreateCanvasPrefab();
            var viewPrefab = CreatePlaceholderViewPrefab();
            CreateBootstrapPrefab(canvasPrefab);
            CreateUiSceneAndExport(viewPrefab);
            CreateMainScene();
            CreateStageEntryAsset();
            CreateScriptableObjectAssets();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MainScenePath, true),
            };

            ResourcesDictionaryBuilder.BuildResourcesDictionary();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Hearthstone project initialization assets created successfully.");
        }

        private static void EnsureCurrentSceneCanBeReplaced()
        {
            var activeScene = SceneManager.GetActiveScene();
            var roots = activeScene.GetRootGameObjects();
            if (!string.IsNullOrEmpty(activeScene.path) && roots.Length > 0)
                throw new InvalidOperationException(
                    $"Refusing to replace the saved active scene '{activeScene.path}'. Open a new empty scene and retry.");

            if (!activeScene.isDirty || roots.Length == 0)
                return;

            if (roots.Length == 2 && IsDefaultNewSceneRoot(roots[0]) && IsDefaultNewSceneRoot(roots[1]))
                return;

            throw new InvalidOperationException(
                "Refusing to replace an unsaved scene containing unknown objects. Save it or open a new empty scene and retry.");
        }

        private static bool IsDefaultNewSceneRoot(GameObject root)
        {
            if (root.name == "Main Camera")
                return root.GetComponent<Camera>() != null;
            if (root.name == "Directional Light")
                return root.GetComponent<Light>() != null;
            return false;
        }

        private static void EnsureCleanTargets()
        {
            var targets = new[]
            {
                CanvasPrefabPath,
                PlaceholderViewPrefabPath,
                BootstrapPrefabPath,
                UiScenePath,
                UiSceneAssetPath,
                MainScenePath,
                EntryAssetPath,
                ScriptableObjectAssetsPath,
            };

            foreach (var target in targets)
            {
                if (AssetDatabase.LoadMainAssetAtPath(target) != null)
                    throw new InvalidOperationException($"Initialization target already exists: {target}");
            }
        }

        private static GameObject CreateCanvasPrefab()
        {
            var canvasRoot = new GameObject("CanvasProto", typeof(RectTransform));
            try
            {
                var canvas = canvasRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasRoot.AddComponent<GraphicRaycaster>();
                return PrefabUtility.SaveAsPrefabAsset(canvasRoot, CanvasPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(canvasRoot);
            }
        }

        private static GameObject CreatePlaceholderViewPrefab()
        {
            var viewRoot = new GameObject("PlaceholderView", typeof(RectTransform));
            try
            {
                var viewRect = (RectTransform)viewRoot.transform;
                viewRect.sizeDelta = new Vector2(640f, 160f);

                var view = viewRoot.AddComponent<PlaceholderView>();
                view.DefaultShow = true;

                var statusObject = new GameObject("StatusText", typeof(RectTransform));
                statusObject.transform.SetParent(viewRoot.transform, false);
                var statusRect = (RectTransform)statusObject.transform;
                statusRect.anchorMin = Vector2.zero;
                statusRect.anchorMax = Vector2.one;
                statusRect.offsetMin = Vector2.zero;
                statusRect.offsetMax = Vector2.zero;

                var statusText = statusObject.AddComponent<TextMeshProUGUI>();
                statusText.text = "Initializing";
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.fontSize = 48f;
                view.StatusText = statusText;

                return PrefabUtility.SaveAsPrefabAsset(viewRoot, PlaceholderViewPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(viewRoot);
            }
        }

        private static void CreateBootstrapPrefab(GameObject canvasPrefab)
        {
            var bootstrapRoot = new GameObject("Bootstrap");
            try
            {
                var engine = bootstrapRoot.AddComponent<HearthstoneGameEngine>();
                engine.UiCanvasProto = canvasPrefab;

                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(bootstrapRoot.transform, false);
                cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
                cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();

                var eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.transform.SetParent(bootstrapRoot.transform, false);
                eventSystemObject.AddComponent<EventSystem>();
                eventSystemObject.AddComponent<StandaloneInputModule>();

                PrefabUtility.SaveAsPrefabAsset(bootstrapRoot, BootstrapPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(bootstrapRoot);
            }
        }

        private static void CreateUiSceneAndExport(GameObject viewPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvasRoot = new GameObject("PlaceholderUiCanvas", typeof(RectTransform));
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasRoot.AddComponent<GraphicRaycaster>();

            var exporter = canvasRoot.AddComponent<UiSceneExporter>();
            exporter.ExportPath = "Assets/Resources/Ui";
            exporter.FullUiGroupType = typeof(PlaceholderUiGroup).FullName;
            exporter.GenerateUiGroups();

            var groupRect = (RectTransform)exporter.UiGroups[0].transform;
            groupRect.anchorMin = new Vector2(0.5f, 0.5f);
            groupRect.anchorMax = new Vector2(0.5f, 0.5f);
            groupRect.sizeDelta = new Vector2(1920f, 1080f);

            var viewInstance = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab, scene);
            viewInstance.transform.SetParent(groupRect, false);
            var viewRect = (RectTransform)viewInstance.transform;
            viewRect.localPosition = Vector3.zero;
            viewRect.localScale = Vector3.one;

            EditorSceneManager.SaveScene(scene, UiScenePath);
            SceneManager.SetActiveScene(scene);
            exporter.ExportUiScene();
        }

        private static void CreateMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            if (bootstrapPrefab == null)
                throw new InvalidOperationException($"Bootstrap prefab was not found at {BootstrapPrefabPath}");

            PrefabUtility.InstantiatePrefab(bootstrapPrefab, scene);
            EditorSceneManager.SaveScene(scene, MainScenePath);
        }

        private static void CreateStageEntryAsset()
        {
            var entry = GameStageWindow.CreateEntryAsset(typeof(InitialStageEntryAsset));
            var path = AssetDatabase.GetAssetPath(entry);
            if (!string.Equals(path, EntryAssetPath, StringComparison.Ordinal))
                throw new InvalidOperationException($"Unexpected Stage entry asset path: {path}");

            var serializedEntry = new SerializedObject(entry);
            serializedEntry.FindProperty("m_DisplayName").stringValue = "Initial Stage";
            serializedEntry.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }

        private static void CreateScriptableObjectAssets()
        {
            EnsureAssetFolder("Assets/Resources/BbxCommon");
            var assets = ScriptableObject.CreateInstance<ScriptableObjectAssets>();
            assets.Assets = new SerializableDic<string, SerializableHashSet<string>>();
            AssetDatabase.CreateAsset(assets, ScriptableObjectAssetsPath);
        }

        private static void EnsureAssetFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var separator = path.LastIndexOf('/');
            var parent = path.Substring(0, separator);
            var folderName = path.Substring(separator + 1);
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
#endif
