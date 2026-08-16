#if UNITY_EDITOR
using System;
using BbxCommon.Ui;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class BattleViewUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/BattleView.prefab";
        private const string BackgroundPath = "Assets/Resources/Art/BattleCards/UI/BattleBoardBackgroundAged.png";
        private const string CornerDaggerPath = "Assets/Resources/Art/BattleCards/UI/BattleCornerDagger.png";
        private const string CornerQuillStampPath =
            "Assets/Resources/Art/BattleCards/UI/BattleCornerQuillStamp.png";
        private const string DividerPath = "Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png";
        private const string BannerPath = "Assets/Resources/Art/BattleCards/Result/BattleVictoryBannerAged.png";
        private const string DefeatPanelPath = "Assets/Resources/Art/BattleCards/Result/BattleDefeatPanelAged.png";
        private const string RunVictoryPanelPath = "Assets/Resources/Art/BattleCards/Result/RunVictoryPanelAged.png";
        private const string ReturnToMainMenuButtonPath =
            "Assets/Resources/Art/BattleCards/Result/ReturnToMainMenuButtonAged.png";

        public static void Build()
        {
            var root = PreparationUiBuilderUtility.CreateUiObject("BattleView", null);
            try
            {
                var rootRect = (RectTransform)root.transform;
                rootRect.sizeDelta = new Vector2(1920f, 1080f);
                var view = root.AddComponent<BattleView>();
                view.DefaultShow = true;

                var background = PreparationUiBuilderUtility.CreateUiObject("BoardBackground", root.transform);
                PreparationUiBuilderUtility.Stretch(background);
                var backgroundImage = PreparationUiBuilderUtility.AddImage(
                    background,
                    LoadSprite(BackgroundPath));
                backgroundImage.preserveAspect = false;

                var parchmentAging = PreparationUiBuilderUtility.CreateUiObject("ParchmentAgingOverlay", root.transform);
                var parchmentRect = (RectTransform)parchmentAging.transform;
                parchmentRect.anchorMin = new Vector2(0.055f, 0.07f);
                parchmentRect.anchorMax = new Vector2(0.945f, 0.93f);
                parchmentRect.offsetMin = Vector2.zero;
                parchmentRect.offsetMax = Vector2.zero;
                var parchmentImage = PreparationUiBuilderUtility.AddImage(
                    parchmentAging,
                    PreparationUiBuilderUtility.LoadSprite("ParchmentAgingOverlay"));
                parchmentImage.preserveAspect = false;
                parchmentImage.color = new Color(1f, 0.89f, 0.67f, 0.42f);
                parchmentImage.raycastTarget = false;

                CreateCornerDecorations(root.transform);

                var divider = PreparationUiBuilderUtility.CreateUiObject("BattleCenterDivider", root.transform);
                PreparationUiBuilderUtility.SetRect(
                    divider,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(1260f, 160f),
                    Vector2.zero);
                var dividerImage = PreparationUiBuilderUtility.AddImage(divider, LoadSprite(DividerPath));
                dividerImage.preserveAspect = false;
                dividerImage.color = new Color(1f, 1f, 1f, 0.58f);
                dividerImage.raycastTarget = false;

                view.EnemyCardList = CreateCardList(root.transform, "EnemyCardList", new Vector2(0f, 285f));
                view.PlayerCardList = CreateCardList(root.transform, "PlayerCardList", new Vector2(0f, -285f));

                CreateVictoryBanner(root.transform, view);
                CreateResultPopup(root.transform, view);

                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, false);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void CreateCornerDecorations(Transform parent)
        {
            var dagger = PreparationUiBuilderUtility.CreateUiObject("UpperLeftDagger", parent);
            var daggerRect = (RectTransform)dagger.transform;
            daggerRect.anchorMin = new Vector2(0f, 1f);
            daggerRect.anchorMax = new Vector2(0f, 1f);
            daggerRect.pivot = new Vector2(0.5f, 0.5f);
            daggerRect.sizeDelta = new Vector2(420f, 630f);
            daggerRect.anchoredPosition = new Vector2(220f, -280f);
            daggerRect.localScale = new Vector3(-1f, 1f, 1f);
            var daggerImage = PreparationUiBuilderUtility.AddImage(dagger, LoadSprite(CornerDaggerPath));
            daggerImage.preserveAspect = true;
            daggerImage.raycastTarget = false;

            var quillStamp = PreparationUiBuilderUtility.CreateUiObject("LowerRightQuillStamp", parent);
            var quillStampRect = (RectTransform)quillStamp.transform;
            quillStampRect.anchorMin = new Vector2(1f, 0f);
            quillStampRect.anchorMax = new Vector2(1f, 0f);
            quillStampRect.pivot = new Vector2(0.5f, 0.5f);
            quillStampRect.sizeDelta = new Vector2(320f, 213f);
            quillStampRect.anchoredPosition = new Vector2(-100f, 110f);
            var quillStampImage = PreparationUiBuilderUtility.AddImage(
                quillStamp,
                LoadSprite(CornerQuillStampPath));
            quillStampImage.preserveAspect = true;
            quillStampImage.raycastTarget = false;
        }

        private static UiList CreateCardList(Transform parent, string name, Vector2 position)
        {
            var listObject = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(
                listObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(1680f, 360f),
                position);
            var list = listObject.AddComponent<UiList>();
            list.ArragementType = UiList.EArrangement.AreaFit;
            list.AreaDirection = UiList.EDirection.Horizontal;
            list.AreaSlotSize = new Vector2(278f, 360f);
            return list;
        }

        private static void CreateVictoryBanner(Transform parent, BattleView view)
        {
            var banner = PreparationUiBuilderUtility.CreateUiObject("VictoryBanner", parent);
            var rect = PreparationUiBuilderUtility.SetRect(
                banner,
                new Vector2(0.5f, 0.5f),
                new Vector2(1200f, 400f),
                new Vector2(-1450f, 0f));
            var image = PreparationUiBuilderUtility.AddImage(banner, LoadSprite(BannerPath));
            image.preserveAspect = true;
            image.raycastTarget = false;
            var canvasGroup = banner.AddComponent<CanvasGroup>();

            view.VictoryBannerRoot = rect;
            view.VictoryBannerCanvasGroup = canvasGroup;
            banner.SetActive(false);
        }

        private static void CreateResultPopup(Transform parent, BattleView view)
        {
            LoadSprite(RunVictoryPanelPath);
            var blocker = PreparationUiBuilderUtility.CreateUiObject("ResultPopup", parent);
            PreparationUiBuilderUtility.Stretch(blocker);
            var blockerImage = PreparationUiBuilderUtility.AddImage(blocker, null, true);
            blockerImage.color = new Color(0f, 0f, 0f, 0.68f);
            var canvasGroup = blocker.AddComponent<CanvasGroup>();

            var panel = PreparationUiBuilderUtility.CreateUiObject("Panel", blocker.transform);
            PreparationUiBuilderUtility.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(740f, 900f), Vector2.zero);
            var panelImage = PreparationUiBuilderUtility.AddImage(panel, LoadSprite(DefeatPanelPath), true);
            panelImage.preserveAspect = true;

            var returnButton = PreparationUiBuilderUtility.CreateUiObject("ReturnToMainMenuButton", panel.transform);
            PreparationUiBuilderUtility.SetRect(
                returnButton,
                new Vector2(0.5f, 0.5f),
                new Vector2(510f, 170f),
                new Vector2(0f, -280f));
            var buttonImage = PreparationUiBuilderUtility.AddImage(
                returnButton,
                LoadSprite(ReturnToMainMenuButtonPath),
                true);
            buttonImage.preserveAspect = true;
            var button = returnButton.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            view.ResultPopupRoot = blocker;
            view.ResultPopupCanvasGroup = canvasGroup;
            view.ResultPopupImage = panelImage;
            view.ReturnToMainMenuButton = button;
            blocker.SetActive(false);
        }

        private static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException($"Battle UI texture is missing at '{path}'.");
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
                ?? throw new InvalidOperationException($"Battle UI Sprite is missing at '{path}'.");
        }
    }
}
#endif
