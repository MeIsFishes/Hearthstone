#if UNITY_EDITOR
using System;
using BbxCommon.Editor;
using BbxCommon.Ui;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class PreparationUiSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Ui/Preparation.unity";
        public const string AssetPath = "Assets/Resources/Ui/Preparation.asset";
        public const string EntryPath = "Assets/Resources/Editor/PreparationStageEntry.asset";
        private const string ViewPrefabPath = "Assets/Resources/Ui/PreparationView.prefab";

        public static void Build()
        {
            var active = SceneManager.GetActiveScene();
            var viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ViewPrefabPath)
                ?? throw new InvalidOperationException($"Preparation View Prefab is missing at '{ViewPrefabPath}'.");
            Scene scene = default;
            try
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
                var canvasRoot = new GameObject("PreparationUiCanvas", typeof(RectTransform));
                var canvas = canvasRoot.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasRoot.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasRoot.AddComponent<GraphicRaycaster>();

                var exporter = canvasRoot.AddComponent<UiSceneExporter>();
                exporter.ExportPath = "Assets/Resources/Ui";
                exporter.FullUiGroupType = typeof(EPreparationUiGroup).FullName;
                exporter.GenerateUiGroups();
                if (exporter.UiGroups == null || exporter.UiGroups.Count != 1)
                    throw new InvalidOperationException("Preparation UI exporter did not create exactly one Main group.");

                var group = (RectTransform)exporter.UiGroups[0].transform;
                group.anchorMin = new Vector2(0.5f, 0.5f);
                group.anchorMax = new Vector2(0.5f, 0.5f);
                group.pivot = new Vector2(0.5f, 0.5f);
                group.sizeDelta = new Vector2(1920f, 1080f);
                group.anchoredPosition = Vector2.zero;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab, scene);
                instance.transform.SetParent(group, false);
                var instanceRect = (RectTransform)instance.transform;
                instanceRect.localPosition = Vector3.zero;
                instanceRect.localScale = Vector3.one;
                instanceRect.pivot = new Vector2(0.5f, 0.5f);

                EditorSceneManager.SaveScene(scene, ScenePath);
                exporter.ExportUiScene();
                if (AssetDatabase.LoadAssetAtPath<UiSceneAsset>(AssetPath) == null)
                    throw new InvalidOperationException($"Preparation UiSceneAsset was not exported to '{AssetPath}'.");

                EnsureEntryAsset();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                if (active.IsValid() && active.isLoaded)
                    SceneManager.SetActiveScene(active);
                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void EnsureEntryAsset()
        {
            var entry = AssetDatabase.LoadAssetAtPath<GameStageEntryAsset>(EntryPath);
            if (entry == null)
            {
                var entryType = Type.GetType("Hearthstone.Editor.PreparationStageEntryAsset, Hearthstone.Editor");
                if (entryType == null)
                    throw new InvalidOperationException("PreparationStageEntryAsset type is unavailable.");
                entry = GameStageWindow.CreateEntryAsset(entryType);
                if (!string.Equals(AssetDatabase.GetAssetPath(entry), EntryPath, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Unexpected Preparation entry path '{AssetDatabase.GetAssetPath(entry)}'.");
            }

            var serialized = new SerializedObject(entry);
            var displayName = serialized.FindProperty("m_DisplayName");
            if (displayName != null)
                displayName.stringValue = "Preparation Stage";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
        }
    }
}
#endif
