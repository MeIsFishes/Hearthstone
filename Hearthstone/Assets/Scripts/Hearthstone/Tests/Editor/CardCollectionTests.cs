using System;
using System.IO;
using System.Linq;
using BbxCommon;
using BbxCommon.Ui;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Hearthstone.Tests
{
    public sealed class CardCollectionTests
    {
        private string m_TemporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            DataApi.ReleaseAllData<BattleCardCsvData>(false);
            DataApi.ReleaseAllData<BattleCardTypeCsvData>(false);
            ResourceApi.Initialize();
            CsvApi.ReadFromString<BattleCardTypeCsvData>(
                nameof(BattleCardTypeCsvData),
                ResourceApi.LoadTextAsset(nameof(BattleCardTypeCsvData)).text);
            CsvApi.ReadFromString<BattleCardCsvData>(
                nameof(BattleCardCsvData),
                ResourceApi.LoadTextAsset(nameof(BattleCardCsvData)).text);
            m_TemporaryDirectory = Path.Combine(Path.GetTempPath(), "99AscensionTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            DataApi.ReleaseAllData<BattleCardCsvData>(false);
            DataApi.ReleaseAllData<BattleCardTypeCsvData>(false);
            if (Directory.Exists(m_TemporaryDirectory))
                Directory.Delete(m_TemporaryDirectory, true);
        }

        [Test]
        public void CatalogExcludesDividerAndEveryFourCardFusion()
        {
            var cards = CardCollectionCatalog.GetCollectibleCardNumbers();
            Assert.AreEqual(147, cards.Count);
            CollectionAssert.DoesNotContain(cards, RunCardRules.LockedCardNumber);
            Assert.IsTrue(cards.All(number =>
                DataApi.GetData<BattleCardCsvData>(number).FusionRecipeTypeIds.Count != 4));
            Assert.AreEqual(98, cards.Count(number => number <= RunCardRules.LastOrdinaryCardNumber));
            Assert.AreEqual(49, cards.Count(number => number >= RunCardRules.FirstFusionCardNumber));
        }

        [Test]
        public void RepositoryPersistsUniqueUnlockedCardsAndRejectsExcludedCards()
        {
            var savePath = Path.Combine(m_TemporaryDirectory, "collection.json");
            var repository = new CardCollectionRepository(savePath);
            Assert.IsTrue(repository.Register(1));
            Assert.IsFalse(repository.Register(1));
            Assert.IsTrue(repository.Register(100));
            Assert.IsFalse(repository.Register(RunCardRules.LockedCardNumber));
            Assert.IsFalse(repository.Register(RunCardRules.FirstLegendaryCardNumber));

            var reloaded = new CardCollectionRepository(savePath);
            CollectionAssert.AreEquivalent(new[] { 1, 100 }, reloaded.GetUnlockedSnapshot());
            reloaded.Clear();
            Assert.IsFalse(File.Exists(savePath));
        }

        [Test]
        public void CollectionPrefabHasPoolCounterAndDismissablePreview()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Ui/CardCollectionView.prefab");
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<CardCollectionView>();
            Assert.NotNull(view);
            Assert.IsFalse(view.DefaultShow);
            Assert.NotNull(view.BackButton);
            Assert.NotNull(view.CollectedCountText);
            Assert.AreEqual("已解锁 0/0", view.CollectedCountText.text);
            Assert.AreEqual(
                new Vector2(1f, 1f),
                ((RectTransform)view.CollectedCountText.transform.parent).anchorMin);
            Assert.AreEqual(TextAlignmentOptions.MidlineRight, view.CollectedCountText.alignment);
            Assert.AreEqual(new Vector2(0f, 1f), ((RectTransform)view.BackButton.transform).anchorMin);
            Assert.Greater(
                view.CollectedCountText.transform.parent.GetSiblingIndex(),
                prefab.transform.Find("CardPoolPanel").GetSiblingIndex());
            Assert.NotNull(view.CardScrollRect);
            Assert.NotNull(view.CardList);
            Assert.AreEqual(UiList.EArrangement.ConstantSlot, view.CardList.ArragementType);
            Assert.NotNull(view.PreviewOverlay);
            Assert.NotNull(view.PreviewDismissButton);
            Assert.NotNull(view.PreviewCardRoot);
            Assert.NotNull(view.PreviewCardList);
            Assert.IsFalse(view.PreviewOverlay.activeSelf);
        }

        [Test]
        public void LockedCollectionCardSuppressesFusionSealText()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(script);
            StringAssert.Contains("ShowLockedPreparationCard();", script.text);
            StringAssert.Contains("HideCollectionLockedText();", script.text);
            StringAssert.Contains("m_View.SkillDescriptionText.text = string.Empty", script.text);
        }

        [Test]
        public void FusionCollectionPreviewUsesSharedDeterministicSimulation()
        {
            var first = BattleCardSimulationFactory.CreateDeterministic(100);
            var second = BattleCardSimulationFactory.CreateDeterministic(100);
            Assert.AreEqual(first, second);
            Assert.AreEqual(100, first.CardNumber);
            Assert.AreEqual(EBattleCardTier.Silver, first.Tier);
            Assert.AreEqual(EBattleKeyword.Taunt, first.Keywords);
            Assert.That(first.Attack, Is.InRange(4, 8));
            Assert.That(first.MaxHealth, Is.InRange(10, 14));

            var cardScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            var startupScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/GameStage/BattleStageStartupData.cs");
            StringAssert.Contains("BattleCardSimulationFactory.CreateDeterministic(cardNumber)", cardScript.text);
            StringAssert.Contains("BattleCardSimulationFactory.Create(cardNumber, ref random)", startupScript.text);
        }

        [Test]
        public void MainMenuContainsCollectionAndRedDebugClearButtons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Ui/MainMenuView.prefab");
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<MainMenuView>();
            Assert.NotNull(view.CollectionButton);
            Assert.AreEqual("图鉴", view.CollectionLabel.text);
            Assert.NotNull(view.ClearDataButton);
            Assert.AreEqual("清除数据", view.ClearDataLabel.text);
            Assert.That(view.ClearDataLabel.color.r, Is.GreaterThan(view.ClearDataLabel.color.g * 3f));
        }

        [Test]
        public void PocketAnimationEndsAtBottomWithPointThreeScale()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/CardCollectionController.cs");
            Assert.NotNull(script);
            StringAssert.Contains("PocketFinalScale = 0.3f", script.text);
            StringAssert.Contains("overlayRect.rect.yMin", script.text);
            StringAssert.Contains("m_View.PreviewDismissButton.onClick", script.text);
            StringAssert.Contains("AudioApi.Play(\"handleSmallLeather\", 0.68f)", script.text);
        }

        [Test]
        public void PreviewOpensFromClickedCardAndFinishesAtScreenCenter()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/CardCollectionController.cs");
            Assert.NotNull(script);
            StringAssert.Contains("PreviewOpenDuration = 0.28f", script.text);
            StringAssert.Contains("m_View.PreviewCardRoot.position = sourceRect.position", script.text);
            StringAssert.Contains("Vector2.zero", script.text);
            StringAssert.Contains("m_Opening", script.text);
            StringAssert.Contains("m_View.PreviewDismissButton.interactable = false", script.text);
            StringAssert.Contains("AudioApi.Play(\"click_001\", 0.7f)", script.text);
            Assert.That(
                script.text.IndexOf("AudioApi.Play(\"click_001\", 0.7f)", StringComparison.Ordinal),
                Is.GreaterThan(script.text.IndexOf("m_Opening = true;", StringComparison.Ordinal)));
        }
    }
}
