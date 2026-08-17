using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone.Tests
{
    public sealed class NewPlayerGuideTests
    {
        private bool m_WasTriggered;

        [SetUp]
        public void SetUp()
        {
            m_WasTriggered = NewPlayerGuideSave.HasTriggered(
                NewPlayerGuideSave.PreparationBasicsGuideId);
        }

        [TearDown]
        public void TearDown()
        {
            if (m_WasTriggered)
                NewPlayerGuideSave.MarkTriggered(NewPlayerGuideSave.PreparationBasicsGuideId);
            else
                NewPlayerGuideSave.Clear();
        }

        [Test]
        public void PreparationGuideProgressCanBeMarkedAndCleared()
        {
            NewPlayerGuideSave.Clear();
            Assert.False(NewPlayerGuideSave.HasTriggered(NewPlayerGuideSave.PreparationBasicsGuideId));

            NewPlayerGuideSave.MarkTriggered(NewPlayerGuideSave.PreparationBasicsGuideId);
            Assert.True(NewPlayerGuideSave.HasTriggered(NewPlayerGuideSave.PreparationBasicsGuideId));

            NewPlayerGuideSave.Clear();
            Assert.False(NewPlayerGuideSave.HasTriggered(NewPlayerGuideSave.PreparationBasicsGuideId));
        }

        [Test]
        public void NewPlayerGuidePrefabContainsThreePagesAndBlockingDimmer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/NewPlayerGuideView.prefab");
            Assert.NotNull(prefab);

            var view = prefab.GetComponent<NewPlayerGuideView>();
            Assert.NotNull(view);
            Assert.NotNull(view.PreviousButton);
            Assert.NotNull(view.NextButton);
            Assert.NotNull(view.PageIndicator);
            Assert.NotNull(view.CardPreviewList);
            Assert.AreEqual(3, view.PageRoots.Length);
            Assert.True(view.PageRoots[0].activeSelf);
            Assert.False(view.PageRoots[1].activeSelf);
            Assert.False(view.PageRoots[2].activeSelf);

            var dimmer = prefab.transform.Find("InputBlockingDimmer");
            Assert.NotNull(dimmer);
            var dimmerImage = dimmer.GetComponent<Image>();
            Assert.NotNull(dimmerImage);
            Assert.True(dimmerImage.raycastTarget);
            Assert.IsNull(dimmer.GetComponent<Button>());
            Assert.That(dimmerImage.color.r, Is.EqualTo(dimmerImage.color.g).Within(0.001f));
            Assert.That(dimmerImage.color.g, Is.EqualTo(dimmerImage.color.b).Within(0.001f));
            Assert.That(dimmerImage.color.a, Is.GreaterThan(0.75f));

            var panel = prefab.transform.Find("ParchmentPanel");
            Assert.NotNull(panel);
            Assert.NotNull(panel.Find("AgedParchmentTexture"));
            Assert.NotNull(panel.Find("TopWood"));
            Assert.NotNull(panel.Find("BottomWood"));
            Assert.NotNull(panel.Find("LeftWood"));
            Assert.NotNull(panel.Find("RightWood"));
        }

        [Test]
        public void GuideIllustrationAndCompleteCardPrefabAreProjectResources()
        {
            Assert.NotNull(Resources.Load<Sprite>("Art/Tutorial/UI/PreparationBattleTurnOrder"));

            var cardPrefab = Resources.Load<GameObject>("Ui/BattleCardItem");
            Assert.NotNull(cardPrefab);
            Assert.NotNull(cardPrefab.GetComponent<BattleCardItemView>());
        }

        [Test]
        public void FusionHelpUsesExistingGuideThirdPageWithoutRewardDismissCallback()
        {
            Assert.AreEqual(2, NewPlayerGuideController.FusionPageIndex);
            Assert.NotNull(typeof(NewPlayerGuideController).GetMethod("ShowPage"));

            var preparationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            Assert.NotNull(preparationPrefab);
            var view = preparationPrefab.GetComponent<PreparationView>();
            Assert.NotNull(view);
            Assert.NotNull(view.FusionHelpButton);
            Assert.IsNull(view.FusionOperationRoot.transform.Find("Title"));

            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains(
                "guide?.ShowPage(NewPlayerGuideController.FusionPageIndex);",
                controllerScript.text);
            StringAssert.Contains(
                "OpenNewPlayerGuide(OnFusionGuideDismissed)",
                controllerScript.text);
        }

        [Test]
        public void FusionHelpButtonUsesSharedParchmentFrameAndIsCenteredBelowSlots()
        {
            var preparationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            Assert.NotNull(preparationPrefab);

            var view = preparationPrefab.GetComponent<PreparationView>();
            Assert.NotNull(view);
            Assert.NotNull(view.FusionHelpButton);
            Assert.NotNull(view.FusionSlotList);

            var helpRect = view.FusionHelpButton.GetComponent<RectTransform>();
            var slotListRect = view.FusionSlotList.GetComponent<RectTransform>();
            Assert.NotNull(helpRect);
            Assert.NotNull(slotListRect);
            Assert.AreEqual(new Vector2(56f, 56f), helpRect.sizeDelta);
            Assert.AreEqual(new Vector2(420f, -275.8f), helpRect.anchoredPosition);
            Assert.That(
                helpRect.anchoredPosition.x,
                Is.EqualTo(slotListRect.anchoredPosition.x).Within(0.001f));

            var slotCenterY = slotListRect.anchoredPosition.y
                              + ((slotListRect.sizeDelta.y
                                  - view.FusionSlotList.ConstantSlotSize.y) * 0.5f);
            var slotBottom = slotCenterY - (view.FusionSlotList.ConstantSlotSize.y * 0.5f);
            var helpTop = helpRect.anchoredPosition.y + (helpRect.sizeDelta.y * 0.5f);
            Assert.That(slotBottom - helpTop, Is.EqualTo(10f).Within(0.001f));

            var image = view.FusionHelpButton.targetGraphic as Image;
            Assert.NotNull(image);
            Assert.AreEqual(
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/Resources/Art/Common/UI/MedievalParchmentControl.png"),
                image.sprite);
            Assert.AreEqual(Color.white, image.color);
            var helpLabel = view.FusionHelpButton.GetComponentInChildren<TMPro.TMP_Text>();
            Assert.AreEqual("?", helpLabel.text);
            Assert.AreEqual(new Vector2(0f, -2f), helpLabel.rectTransform.anchoredPosition);
        }

        [Test]
        public void FusionGuideExplainsKeywordUpgradeAndShowsBoldStrategyHintAtBottom()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/NewPlayerGuideView.prefab");
            Assert.NotNull(prefab);

            var fusionPage = prefab.transform.Find("ParchmentPanel/Pages/FusionPage");
            Assert.NotNull(fusionPage);

            var upgradeHint = fusionPage.Find("KeywordUpgradeHint")?.GetComponent<TMP_Text>();
            Assert.NotNull(upgradeHint);
            Assert.AreEqual("相同的基础词条叠加时会获得升级。", upgradeHint.text);
            Assert.AreEqual(FontStyles.Normal, upgradeHint.fontStyle);

            var strategyHint = fusionPage.Find("FusionStrategyHint")?.GetComponent<TMP_Text>();
            Assert.NotNull(strategyHint);
            Assert.AreEqual(
                "由于多卡融合更加强力，有时候留一手或许是更好的选择！",
                strategyHint.text);
            Assert.AreEqual(upgradeHint.font, strategyHint.font);
            Assert.True((strategyHint.fontStyle & FontStyles.Bold) != 0);
            Assert.That(strategyHint.fontSize, Is.EqualTo(31f).Within(0.001f));

            var strategyRect = strategyHint.rectTransform;
            Assert.That(strategyRect.anchorMin.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(strategyRect.anchorMax.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(strategyRect.anchoredPosition.y, Is.EqualTo(20f).Within(0.001f));
            Assert.That(strategyRect.sizeDelta.y, Is.EqualTo(40f).Within(0.001f));
        }
    }
}
