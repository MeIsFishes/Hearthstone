using System;
using System.Collections.Generic;
using BbxCommon.Ui;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Hearthstone
{
    internal static class PreparationUiBuilderUtility
    {
        internal const string ArtRoot = "Assets/Resources/Art/Preparation/UI/";
        private const string ChineseFontAssetPath = "Assets/Resources/Fonts/NotoSansSC-Dynamic SDF.asset";
        private const string RequiredChineseCharacters =
            "备战阶段本轮获得张卡槽位池哥布林战士弓手投弹野猪食人魔融合造物出战素材已选合计继续下一关嘲讽远射爆裂冲锋";
        internal const float CardPoolViewportWidth = 1400f;
        internal static float CardPoolCellWidth => CardPoolViewportWidth / RunCardRules.CardsPerRow;
        internal static float CardPoolCellHeight =>
            CardPoolCellWidth * RunCardRules.CardAspectHeight / RunCardRules.CardAspectWidth;
        private static readonly Dictionary<int, Scene> BuilderScenes = new();
        private static TMP_FontAsset s_ChineseFont;
        private static bool s_ChineseFontValidated;

        internal static GameObject CreateUiObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                gameObject.transform.SetParent(parent, false);
            else
            {
                var previewScene = EditorSceneManager.NewPreviewScene();
                SceneManager.MoveGameObjectToScene(gameObject, previewScene);
                BuilderScenes.Add(gameObject.GetInstanceID(), previewScene);
            }
            return gameObject;
        }

        internal static RectTransform Stretch(GameObject gameObject, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            var rect = (RectTransform)gameObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        internal static RectTransform SetRect(
            GameObject gameObject,
            Vector2 anchor,
            Vector2 size,
            Vector2 position)
        {
            var rect = (RectTransform)gameObject.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        internal static Image AddImage(GameObject gameObject, Sprite sprite, bool raycast = false, Image.Type type = Image.Type.Simple)
        {
            var image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.preserveAspect = type == Image.Type.Simple;
            image.raycastTarget = raycast;
            return image;
        }

        internal static TextMeshProUGUI AddText(
            GameObject gameObject,
            string text,
            float size,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var label = gameObject.AddComponent<TextMeshProUGUI>();
            label.font = LoadChineseFont();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.alignment = alignment;
            label.color = new Color(1f, 0.88f, 0.55f, 1f);
            label.raycastTarget = false;
            return label;
        }

        private static TMP_FontAsset LoadChineseFont()
        {
            if (s_ChineseFont == null)
            {
                s_ChineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontAssetPath)
                    ?? throw new InvalidOperationException(
                        $"Preparation Chinese TMP FontAsset is missing at '{ChineseFontAssetPath}'.");
            }
            if (!s_ChineseFontValidated)
            {
                if (!s_ChineseFont.HasCharacters(RequiredChineseCharacters) &&
                    (!s_ChineseFont.TryAddCharacters(RequiredChineseCharacters, out var missingCharacters) ||
                     !string.IsNullOrEmpty(missingCharacters)))
                {
                    throw new InvalidOperationException(
                        $"Preparation Chinese TMP FontAsset is missing glyphs: '{missingCharacters}'.");
                }
                EditorUtility.SetDirty(s_ChineseFont);
                AssetDatabase.SaveAssets();
                s_ChineseFontValidated = true;
            }
            return s_ChineseFont;
        }

        internal static Sprite LoadSprite(string fileName)
        {
            var path = ArtRoot + fileName + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Preparation UI texture is missing at '{path}'.");
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.alphaIsTransparency == false ||
                importer.mipmapEnabled ||
                importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path)
                ?? throw new InvalidOperationException($"Preparation UI Sprite is missing at '{path}'.");
        }

        internal static Sprite LoadExistingSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path)
                ?? throw new InvalidOperationException($"Required Sprite is missing at '{path}'.");
        }

        internal static void PreInitialize(UiViewBase view)
        {
            UiApi.EditorOperation.PreInitializeView(view);
        }

        internal static void ExportPreload(string prefabPath)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)
                ?? throw new InvalidOperationException($"Prefab is missing at '{prefabPath}'.");
            var view = root.GetComponent<UiViewBase>()
                ?? throw new InvalidOperationException($"View is missing from '{prefabPath}'.");
            UiApi.EditorOperation.ExportPreloadedView(view);
            AssetDatabase.SaveAssets();
        }

        internal static void SavePrefab(GameObject root, string path, bool exportPreload)
        {
            var view = root.GetComponent<UiViewBase>()
                ?? throw new InvalidOperationException($"View is missing from '{root.name}'.");
            PreInitialize(view);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            if (exportPreload)
                ExportPreload(path);
        }

        internal static void DestroyTemporary(GameObject root)
        {
            if (root == null)
                return;
            var instanceId = root.GetInstanceID();
            if (BuilderScenes.TryGetValue(instanceId, out var previewScene))
            {
                BuilderScenes.Remove(instanceId);
                EditorSceneManager.ClosePreviewScene(previewScene);
                return;
            }
            Object.DestroyImmediate(root);
        }
    }
}
