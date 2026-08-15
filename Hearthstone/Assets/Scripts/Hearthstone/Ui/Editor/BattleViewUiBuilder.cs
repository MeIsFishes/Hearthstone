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
        private const string BackgroundPath = "Assets/Resources/Art/BattleCards/UI/BattleBoardBackground.png";
        private const string DividerPath = "Assets/Resources/Art/BattleCards/UI/BattleCenterDividerCarving.png";
        private const string BannerPath = "Assets/Resources/Art/BattleCards/Result/BattleVictoryBanner.png";
        private const string DefeatPanelPath = "Assets/Resources/Art/BattleCards/Result/BattleDefeatPanel.png";
        private const string RunVictoryPanelPath = "Assets/Resources/Art/BattleCards/Result/RunVictoryPanel.png";

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
                parchmentImage.color = new Color(1f, 0.89f, 0.67f, 0.24f);
                parchmentImage.raycastTarget = false;

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
                new Vector2(1040f, 360f),
                new Vector2(-1450f, 0f));
            var image = PreparationUiBuilderUtility.AddImage(banner, LoadSprite(BannerPath));
            image.preserveAspect = true;
            image.raycastTarget = false;
            var canvasGroup = banner.AddComponent<CanvasGroup>();

            var label = PreparationUiBuilderUtility.CreateUiObject("Label", banner.transform);
            PreparationUiBuilderUtility.SetRect(label, new Vector2(0.5f, 0.5f), new Vector2(620f, 110f), new Vector2(0f, -5f));
            var text = PreparationUiBuilderUtility.AddText(label, "战斗胜利", 68f);
            text.color = new Color(0.28f, 0.12f, 0.03f, 1f);

            view.VictoryBannerRoot = rect;
            view.VictoryBannerCanvasGroup = canvasGroup;
            view.VictoryBannerText = text;
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
            PreparationUiBuilderUtility.SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(900f, 650f), Vector2.zero);
            var panelImage = PreparationUiBuilderUtility.AddImage(panel, LoadSprite(DefeatPanelPath), true);
            panelImage.preserveAspect = true;

            var title = PreparationUiBuilderUtility.CreateUiObject("Title", panel.transform);
            PreparationUiBuilderUtility.SetRect(title, new Vector2(0.5f, 0.5f), new Vector2(560f, 90f), new Vector2(0f, 115f));
            var titleText = PreparationUiBuilderUtility.AddText(title, "战斗失败", 58f);
            titleText.color = new Color(0.25f, 0.08f, 0.03f, 1f);

            var body = PreparationUiBuilderUtility.CreateUiObject("Body", panel.transform);
            PreparationUiBuilderUtility.SetRect(body, new Vector2(0.5f, 0.5f), new Vector2(600f, 70f), new Vector2(0f, 25f));
            var bodyText = PreparationUiBuilderUtility.AddText(body, "本局冒险已经结束", 32f);
            bodyText.color = new Color(0.24f, 0.13f, 0.07f, 1f);

            var restart = PreparationUiBuilderUtility.CreateUiObject("RestartButton", panel.transform);
            PreparationUiBuilderUtility.SetRect(restart, new Vector2(0.5f, 0.5f), new Vector2(330f, 92f), new Vector2(0f, -175f));
            var restartImage = PreparationUiBuilderUtility.AddImage(restart, null, true);
            restartImage.color = new Color(0.62f, 0.12f, 0.08f, 0.96f);
            var outline = restart.AddComponent<Outline>();
            outline.effectColor = new Color(0.95f, 0.72f, 0.25f, 1f);
            outline.effectDistance = new Vector2(3f, -3f);
            var button = restart.AddComponent<Button>();
            button.targetGraphic = restartImage;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var restartLabel = PreparationUiBuilderUtility.CreateUiObject("Label", restart.transform);
            PreparationUiBuilderUtility.Stretch(restartLabel);
            var restartText = PreparationUiBuilderUtility.AddText(restartLabel, "重新开始", 38f);

            view.ResultPopupRoot = blocker;
            view.ResultPopupCanvasGroup = canvasGroup;
            view.ResultPopupImage = panelImage;
            view.ResultPopupTitle = titleText;
            view.ResultPopupBody = bodyText;
            view.RestartButton = button;
            view.RestartButtonText = restartText;
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
