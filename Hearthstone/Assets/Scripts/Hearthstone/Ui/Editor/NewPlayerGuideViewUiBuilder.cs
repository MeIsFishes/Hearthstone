using System;
using BbxCommon.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class NewPlayerGuideViewUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/NewPlayerGuideView.prefab";
        private const string ParchmentTexturePath =
            "Assets/Resources/Art/Preparation/UI/ParchmentAgingOverlay.png";
        private const string TurnOrderIllustrationPath =
            "Assets/Resources/Art/Tutorial/UI/PreparationBattleTurnOrder.png";
        private const string CardFramePath =
            "Assets/Resources/Art/BattleCards/UI/CardFrameRoundedSubtleOpenCornersPreview.png";
        private const string CardNumberBadgePath =
            "Assets/Resources/Art/BattleCards/UI/CardNumberBadgeHex.png";
        private const string FusionCardArtworkPath =
            "Assets/Resources/Art/BattleCards/FusionCard_099.png";

        private static readonly Color ParchmentColor = new(0.82f, 0.66f, 0.39f, 1f);
        private static readonly Color InkColor = new(0.19f, 0.085f, 0.028f, 1f);
        private static readonly Color MutedInkColor = new(0.29f, 0.15f, 0.07f, 1f);
        private static readonly Color WoodDark = new(0.15f, 0.06f, 0.018f, 1f);
        private static readonly Color WoodLight = new(0.38f, 0.18f, 0.055f, 1f);

        public static void Build()
        {
            var parchmentTexture = PreparationUiBuilderUtility.LoadSpriteAtPath(ParchmentTexturePath);
            var turnOrderIllustration = PreparationUiBuilderUtility.LoadSpriteAtPath(TurnOrderIllustrationPath);
            var cardFrame = PreparationUiBuilderUtility.LoadSpriteAtPath(CardFramePath);
            var cardNumberBadge = PreparationUiBuilderUtility.LoadSpriteAtPath(CardNumberBadgePath);
            var fusionCardArtwork = PreparationUiBuilderUtility.LoadSpriteAtPath(FusionCardArtworkPath);

            var root = PreparationUiBuilderUtility.CreateUiObject("NewPlayerGuideView", null);
            try
            {
                PreparationUiBuilderUtility.Stretch(root);
                var view = root.AddComponent<NewPlayerGuideView>();

                CreateScreenDimmer(root.transform);
                var panel = CreatePanel(root.transform, parchmentTexture);
                CreateTitle(panel.transform);

                var content = PreparationUiBuilderUtility.CreateUiObject("Pages", panel.transform);
                PreparationUiBuilderUtility.SetRect(
                    content,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(1420f, 650f),
                    new Vector2(0f, 35f));

                var firstPage = CreateCardAttributePage(content.transform, out var cardPreviewList);
                var secondPage = CreateTurnOrderPage(
                    content.transform,
                    turnOrderIllustration,
                    cardNumberBadge);
                var thirdPage = CreateFusionPage(
                    content.transform,
                    cardFrame,
                    cardNumberBadge,
                    fusionCardArtwork);

                CreateFooter(panel.transform, view);
                view.PageRoots = new[] { firstPage, secondPage, thirdPage };
                view.CardPreviewList = cardPreviewList;
                firstPage.SetActive(true);
                secondPage.SetActive(false);
                thirdPage.SetActive(false);

                PreparationUiBuilderUtility.SavePrefab(root, PrefabPath, true);
            }
            finally
            {
                PreparationUiBuilderUtility.DestroyTemporary(root);
            }
        }

        private static void CreateScreenDimmer(Transform parent)
        {
            var dimmer = PreparationUiBuilderUtility.CreateUiObject("InputBlockingDimmer", parent);
            PreparationUiBuilderUtility.Stretch(dimmer);
            var image = PreparationUiBuilderUtility.AddImage(dimmer, null, true);
            image.color = new Color(0.27f, 0.27f, 0.27f, 0.82f);
        }

        private static GameObject CreatePanel(Transform parent, Sprite parchmentTexture)
        {
            var panel = PreparationUiBuilderUtility.CreateUiObject("ParchmentPanel", parent);
            PreparationUiBuilderUtility.SetRect(
                panel,
                new Vector2(0.5f, 0.5f),
                new Vector2(1540f, 900f),
                Vector2.zero);
            var baseImage = PreparationUiBuilderUtility.AddImage(panel, null, true);
            baseImage.color = ParchmentColor;

            var texture = PreparationUiBuilderUtility.CreateUiObject("AgedParchmentTexture", panel.transform);
            PreparationUiBuilderUtility.Stretch(texture, 12f, 12f, 12f, 12f);
            var textureImage = PreparationUiBuilderUtility.AddImage(texture, parchmentTexture);
            textureImage.preserveAspect = false;
            textureImage.color = new Color(1f, 0.91f, 0.72f, 0.78f);

            CreateWoodFrame(panel.transform);
            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(12f, -14f);
            shadow.useGraphicAlpha = true;
            return panel;
        }

        private static void CreateWoodFrame(Transform parent)
        {
            CreateFrameStrip(parent, "TopWood", new Vector2(0.5f, 1f), new Vector2(1540f, 14f), new Vector2(0f, -7f), WoodLight);
            CreateFrameStrip(parent, "BottomWood", new Vector2(0.5f, 0f), new Vector2(1540f, 14f), new Vector2(0f, 7f), WoodDark);
            CreateFrameStrip(parent, "LeftWood", new Vector2(0f, 0.5f), new Vector2(14f, 900f), new Vector2(7f, 0f), WoodDark);
            CreateFrameStrip(parent, "RightWood", new Vector2(1f, 0.5f), new Vector2(14f, 900f), new Vector2(-7f, 0f), WoodLight);

            CreateFrameStrip(parent, "TopInnerBevel", new Vector2(0.5f, 1f), new Vector2(1512f, 3f), new Vector2(0f, -18f), WoodDark);
            CreateFrameStrip(parent, "BottomInnerBevel", new Vector2(0.5f, 0f), new Vector2(1512f, 3f), new Vector2(0f, 18f), WoodLight);
        }

        private static void CreateFrameStrip(
            Transform parent,
            string name,
            Vector2 anchor,
            Vector2 size,
            Vector2 position,
            Color color)
        {
            var strip = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(strip, anchor, size, position);
            PreparationUiBuilderUtility.AddImage(strip, null).color = color;
        }

        private static void CreateTitle(Transform parent)
        {
            var title = PreparationUiBuilderUtility.CreateUiObject("Title", parent);
            PreparationUiBuilderUtility.SetRect(
                title,
                new Vector2(0.5f, 1f),
                new Vector2(720f, 72f),
                new Vector2(0f, -58f));
            var label = PreparationUiBuilderUtility.AddText(title, "新手引导", 48f);
            label.fontStyle = FontStyles.Bold;
            label.color = InkColor;
        }

        private static GameObject CreatePageRoot(string name, Transform parent)
        {
            var page = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.Stretch(page);
            return page;
        }

        private static GameObject CreateCardAttributePage(Transform parent, out UiList cardPreviewList)
        {
            var page = CreatePageRoot("CardAttributesPage", parent);
            CreatePageHeader(page.transform, "如何查看卡牌属性");

            var cardRoot = PreparationUiBuilderUtility.CreateUiObject("CompleteCardPrefab", page.transform);
            PreparationUiBuilderUtility.SetRect(
                cardRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(330f, 500f),
                new Vector2(-425f, -18f));
            cardPreviewList = cardRoot.AddComponent<UiList>();
            cardPreviewList.ArragementType = UiList.EArrangement.Manual;

            CreateCallout(page.transform, "① 左上角是卡牌点数", new Vector2(260f, 148f));
            CreateCallout(page.transform, "② 中间展示卡牌形象与名称", new Vector2(260f, 64f));
            CreateCallout(page.transform, "③ 左下为攻击力，右下为生命值", new Vector2(260f, -20f));
            CreateCallout(page.transform, "④ 将鼠标悬停在卡牌上，可查看词条说明", new Vector2(260f, -104f));

            return page;
        }

        private static void CreateCallout(Transform parent, string text, Vector2 position)
        {
            var callout = PreparationUiBuilderUtility.CreateUiObject("Callout", parent);
            PreparationUiBuilderUtility.SetRect(
                callout,
                new Vector2(0.5f, 0.5f),
                new Vector2(800f, 68f),
                position);
            var background = PreparationUiBuilderUtility.AddImage(callout, null);
            background.color = new Color(0.25f, 0.11f, 0.025f, 0.1f);
            var label = PreparationUiBuilderUtility.CreateUiObject("Label", callout.transform);
            PreparationUiBuilderUtility.Stretch(label, 24f, 8f, 18f, 8f);
            var textLabel = PreparationUiBuilderUtility.AddText(
                label,
                text,
                31f,
                TextAlignmentOptions.MidlineLeft);
            textLabel.color = InkColor;
        }

        private static GameObject CreateTurnOrderPage(
            Transform parent,
            Sprite illustration,
            Sprite numberBadge)
        {
            var page = CreatePageRoot("TurnOrderPage", parent);
            CreatePageHeader(page.transform, "战斗顺序与轮次");

            var art = PreparationUiBuilderUtility.CreateUiObject("TurnOrderIllustration", page.transform);
            PreparationUiBuilderUtility.SetRect(
                art,
                new Vector2(0.5f, 0.5f),
                new Vector2(1260f, 420f),
                new Vector2(0f, 0f));
            var artImage = PreparationUiBuilderUtility.AddImage(art, illustration);
            artImage.preserveAspect = true;
            var outline = art.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.07f, 0.018f, 0.8f);
            outline.effectDistance = new Vector2(3f, -3f);

            CreateTurnSequenceBadge(art.transform, numberBadge, 1, new Vector2(-350f, -55f));
            CreateTurnSequenceBadge(art.transform, numberBadge, 2, new Vector2(-350f, 155f));
            CreateTurnSequenceBadge(art.transform, numberBadge, 3, new Vector2(-115f, -55f));
            CreateTurnSequenceBadge(art.transform, numberBadge, 4, new Vector2(-115f, 155f));
            CreateTurnSequenceBadge(art.transform, numberBadge, 5, new Vector2(115f, -55f));
            CreateTurnSequenceBadge(art.transform, numberBadge, 6, new Vector2(115f, 155f));
            CreateTurnSequenceBadge(art.transform, numberBadge, 7, new Vector2(350f, -55f));
            CreateTurnSequenceBadge(art.transform, numberBadge, 8, new Vector2(350f, 155f));

            CreateBodyText(
                page.transform,
                "我方先手，敌我交替行动；每个阵营都按槽位从左到右轮转，跳过已阵亡卡牌。\n轮到末尾后从左侧开始下一轮，直到一方全部阵亡。",
                new Vector2(0f, -255f),
                new Vector2(1320f, 96f),
                29f);
            return page;
        }

        private static void CreateTurnSequenceBadge(
            Transform parent,
            Sprite sprite,
            int sequence,
            Vector2 position)
        {
            var badge = PreparationUiBuilderUtility.CreateUiObject("TurnSequence" + sequence, parent);
            PreparationUiBuilderUtility.SetRect(
                badge,
                new Vector2(0.5f, 0.5f),
                new Vector2(70f, 70f),
                position);
            PreparationUiBuilderUtility.AddImage(badge, sprite);
            var labelObject = PreparationUiBuilderUtility.CreateUiObject("Label", badge.transform);
            PreparationUiBuilderUtility.Stretch(labelObject, 4f, 4f, 4f, 4f);
            var label = PreparationUiBuilderUtility.AddText(labelObject, sequence.ToString(), 33f);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
        }

        private static GameObject CreateFusionPage(
            Transform parent,
            Sprite cardFrame,
            Sprite cardNumberBadge,
            Sprite fusionArtwork)
        {
            var page = CreatePageRoot("FusionPage", parent);
            CreatePageHeader(page.transform, "点数合计 99 即可融合");

            CreateNumberBadge(page.transform, cardNumberBadge, "25", -520f);
            CreateEquationSymbol(page.transform, "+", -385f);
            CreateNumberBadge(page.transform, cardNumberBadge, "33", -250f);
            CreateEquationSymbol(page.transform, "+", -115f);
            CreateNumberBadge(page.transform, cardNumberBadge, "41", 20f);
            CreateEquationSymbol(page.transform, "=", 155f);
            CreateNumberBadge(page.transform, cardNumberBadge, "99", 290f);
            CreateEquationSymbol(page.transform, ">", 420f);
            CreateFusionResultCard(page.transform, cardFrame, cardNumberBadge, fusionArtwork);

            CreateBodyText(
                page.transform,
                "把左上角点数之和恰好为 99 的卡牌放入融合槽。",
                new Vector2(-245f, -166f),
                new Vector2(900f, 54f),
                29f);
            var result = CreateBodyText(
                page.transform,
                "融合后：攻击与生命直接相加 · 所有词条都会保留",
                new Vector2(0f, -217f),
                new Vector2(1320f, 40f),
                30f,
                "FusionResultSummary");
            result.fontStyle = FontStyles.Bold;

            CreateBodyText(
                page.transform,
                "相同的基础词条叠加时会获得升级。",
                new Vector2(0f, -258f),
                new Vector2(1320f, 40f),
                28f,
                "KeywordUpgradeHint");

            var strategyHintObject = PreparationUiBuilderUtility.CreateUiObject(
                "FusionStrategyHint",
                page.transform);
            PreparationUiBuilderUtility.SetRect(
                strategyHintObject,
                new Vector2(0.5f, 0f),
                new Vector2(1320f, 40f),
                new Vector2(0f, 20f));
            var strategyHint = PreparationUiBuilderUtility.AddText(
                strategyHintObject,
                "由于多卡融合更加强力，有时候留一手或许是更好的选择！",
                31f);
            strategyHint.fontStyle = FontStyles.Bold;
            strategyHint.color = InkColor;
            return page;
        }

        private static void CreateNumberBadge(Transform parent, Sprite sprite, string number, float x)
        {
            var badge = PreparationUiBuilderUtility.CreateUiObject("Number" + number, parent);
            PreparationUiBuilderUtility.SetRect(
                badge,
                new Vector2(0.5f, 0.5f),
                new Vector2(118f, 118f),
                new Vector2(x, 38f));
            PreparationUiBuilderUtility.AddImage(badge, sprite);
            var labelObject = PreparationUiBuilderUtility.CreateUiObject("Label", badge.transform);
            PreparationUiBuilderUtility.Stretch(labelObject, 10f, 10f, 10f, 10f);
            var label = PreparationUiBuilderUtility.AddText(labelObject, number, 44f);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
        }

        private static void CreateEquationSymbol(Transform parent, string symbol, float x)
        {
            var symbolObject = PreparationUiBuilderUtility.CreateUiObject("Symbol", parent);
            PreparationUiBuilderUtility.SetRect(
                symbolObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(100f, 100f),
                new Vector2(x, 38f));
            var label = PreparationUiBuilderUtility.AddText(symbolObject, symbol, 48f);
            label.fontStyle = FontStyles.Bold;
            label.color = InkColor;
        }

        private static void CreateFusionResultCard(
            Transform parent,
            Sprite frame,
            Sprite badge,
            Sprite artwork)
        {
            var card = PreparationUiBuilderUtility.CreateUiObject("FusionResultCard", parent);
            PreparationUiBuilderUtility.SetRect(
                card,
                new Vector2(0.5f, 0.5f),
                new Vector2(180f, 260f),
                new Vector2(570f, 32f));

            var artObject = PreparationUiBuilderUtility.CreateUiObject("Artwork", card.transform);
            PreparationUiBuilderUtility.Stretch(artObject, 20f, 38f, 20f, 28f);
            PreparationUiBuilderUtility.AddImage(artObject, artwork);

            var frameObject = PreparationUiBuilderUtility.CreateUiObject("Frame", card.transform);
            PreparationUiBuilderUtility.Stretch(frameObject);
            PreparationUiBuilderUtility.AddImage(frameObject, frame);

            var badgeObject = PreparationUiBuilderUtility.CreateUiObject("NumberBadge", card.transform);
            PreparationUiBuilderUtility.SetRect(
                badgeObject,
                new Vector2(0f, 1f),
                new Vector2(64f, 64f),
                new Vector2(24f, -28f));
            PreparationUiBuilderUtility.AddImage(badgeObject, badge);
            var labelObject = PreparationUiBuilderUtility.CreateUiObject("Label", badgeObject.transform);
            PreparationUiBuilderUtility.Stretch(labelObject, 4f, 4f, 4f, 4f);
            var label = PreparationUiBuilderUtility.AddText(labelObject, "99", 26f);
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
        }

        private static void CreatePageHeader(Transform parent, string text)
        {
            var header = PreparationUiBuilderUtility.CreateUiObject("PageHeader", parent);
            PreparationUiBuilderUtility.SetRect(
                header,
                new Vector2(0.5f, 1f),
                new Vector2(1100f, 70f),
                new Vector2(0f, -42f));
            var label = PreparationUiBuilderUtility.AddText(header, text, 42f);
            label.fontStyle = FontStyles.Bold;
            label.color = InkColor;
        }

        private static TMP_Text CreateBodyText(
            Transform parent,
            string text,
            Vector2 position,
            Vector2 size,
            float fontSize,
            string objectName = "BodyText")
        {
            var labelObject = PreparationUiBuilderUtility.CreateUiObject(objectName, parent);
            PreparationUiBuilderUtility.SetRect(
                labelObject,
                new Vector2(0.5f, 0.5f),
                size,
                position);
            var label = PreparationUiBuilderUtility.AddText(labelObject, text, fontSize);
            label.color = MutedInkColor;
            label.enableWordWrapping = true;
            return label;
        }

        private static void CreateFooter(Transform parent, NewPlayerGuideView view)
        {
            view.PreviousButton = CreateFooterButton(
                parent,
                "PreviousButton",
                "上一页",
                new Vector2(-510f, 46f),
                out var previousLabel);
            view.PreviousButtonLabel = previousLabel;

            view.NextButton = CreateFooterButton(
                parent,
                "NextButton",
                "下一页",
                new Vector2(510f, 46f),
                out var nextLabel);
            view.NextButtonLabel = nextLabel;

            var indicator = PreparationUiBuilderUtility.CreateUiObject("PageIndicator", parent);
            PreparationUiBuilderUtility.SetRect(
                indicator,
                new Vector2(0.5f, 0f),
                new Vector2(260f, 70f),
                new Vector2(0f, 48f));
            var pageText = PreparationUiBuilderUtility.AddText(indicator, "1 / 3", 32f);
            pageText.fontStyle = FontStyles.Bold;
            pageText.color = InkColor;
            view.PageIndicator = pageText;
        }

        private static Button CreateFooterButton(
            Transform parent,
            string name,
            string text,
            Vector2 position,
            out TMP_Text label)
        {
            var root = PreparationUiBuilderUtility.CreateUiObject(name, parent);
            PreparationUiBuilderUtility.SetRect(
                root,
                new Vector2(0.5f, 0f),
                new Vector2(300f, 84f),
                position);
            var button = PreparationUiBuilderUtility.AddMedievalParchmentButton(root, out _);
            var labelObject = PreparationUiBuilderUtility.CreateUiObject("Label", root.transform);
            PreparationUiBuilderUtility.Stretch(labelObject, 20f, 10f, 20f, 10f);
            label = PreparationUiBuilderUtility.AddText(labelObject, text, 31f);
            label.fontStyle = FontStyles.Bold;
            label.color = InkColor;
            return button;
        }
    }
}
