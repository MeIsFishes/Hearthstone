using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public void RegisterManyAndGetNewReportsOnlyCardsMissingBeforeTheBatch()
        {
            var savePath = Path.Combine(m_TemporaryDirectory, "collection.json");
            var repository = new CardCollectionRepository(savePath);
            Assert.IsTrue(repository.Register(1));

            var newlyUnlocked = repository.RegisterManyAndGetNew(new[]
            {
                1,
                2,
                2,
                RunCardRules.LockedCardNumber,
            });

            CollectionAssert.AreEquivalent(new[] { 2 }, newlyUnlocked);
            Assert.IsEmpty(repository.RegisterManyAndGetNew(new[] { 1, 2 }));
        }

        [Test]
        public void PreparationSessionMarksNewCollectionCardsOnlyForNewlyAppliedDraw()
        {
            var batch = new PreparationRewardBatchStartupData(
                "new-collection-draw",
                new[]
                {
                    new RewardCardGrantStartupData(1, 2, 3),
                    new RewardCardGrantStartupData(2, 3, 4),
                });
            var session = new PreparationSessionSingletonRawComponent();

            session.Initialize(batch, true, new HashSet<int> { 2 });
            CollectionAssert.AreEqual(
                new[] { false, true },
                session.RewardCardsAreNewCollectionEntries);

            session.Initialize(batch, false, new HashSet<int> { 1, 2 });
            CollectionAssert.AreEqual(
                new[] { false, false },
                session.RewardCardsAreNewCollectionEntries);
        }

        [Test]
        public void FourCardFusionUnlocksItsThreeCardPresentationInCollection()
        {
            var savePath = Path.Combine(m_TemporaryDirectory, "collection.json");
            var repository = new CardCollectionRepository(savePath);
            var legendaryResult = new RunCardInstanceData(
                184,
                11,
                15,
                EBattleKeyword.None,
                EBattleCardTier.Legendary,
                131);

            Assert.IsTrue(repository.RegisterFusionResult(legendaryResult));
            Assert.IsTrue(repository.IsUnlocked(131));
            Assert.IsFalse(repository.IsUnlocked(184));
            CollectionAssert.AreEquivalent(new[] { 131 }, repository.GetUnlockedSnapshot());
        }

        [Test]
        public void OwnedLegendaryFusionRestoresItsThreeCardPresentationUnlock()
        {
            var savePath = Path.Combine(m_TemporaryDirectory, "collection.json");
            var repository = new CardCollectionRepository(savePath);
            var runState = new RunStateSingletonRawComponent();
            runState.CardInstances[184] = new RunCardInstanceData(
                184,
                11,
                15,
                EBattleKeyword.None,
                EBattleCardTier.Legendary,
                131);

            Assert.IsTrue(repository.RegisterOwnedCards(runState));
            Assert.IsTrue(repository.IsUnlocked(131));
            Assert.IsFalse(repository.IsUnlocked(184));
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
        public void CardPrefabUsesIndependentHoverClickAndDragEventSources()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            Assert.NotNull(view.CardHoverListener);
            Assert.NotNull(view.CardClickListener);
            Assert.NotNull(view.CardDragListener);
            Assert.AreNotSame(view.CardHoverListener, view.CardClickListener);
            Assert.AreNotSame(view.CardHoverListener, view.CardDragListener);
            Assert.AreNotSame(view.CardClickListener, view.CardDragListener);
            Assert.AreSame(view.CardDragListener, view.PreparationDragable.EventListener);
            Assert.IsFalse(view.CardHoverListener.enabled);
            Assert.IsFalse(view.CardClickListener.enabled);
            Assert.IsFalse(view.CardDragListener.enabled);
        }

        [Test]
        public void CardPrefabHasTransparentGoldNewCollectionNoticeBelowCard()
        {
            const string noticePath =
                "Assets/Resources/Art/BattleCards/UI/NewCollectionNotice.png";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            Assert.NotNull(view.NewCollectionNotice);
            Assert.NotNull(view.NewCollectionNotice.sprite);
            Assert.AreEqual("NewCollectionNotice", view.NewCollectionNotice.sprite.name);
            Assert.IsFalse(view.NewCollectionNotice.gameObject.activeSelf);
            Assert.IsFalse(view.NewCollectionNotice.raycastTarget);
            Assert.AreEqual(new Vector2(0.5f, 0f), view.NewCollectionNotice.rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(0f, 14f), view.NewCollectionNotice.rectTransform.anchoredPosition);
            Assert.AreEqual(new Vector2(180f, 76f), view.NewCollectionNotice.rectTransform.sizeDelta);

            var importer = AssetImporter.GetAtPath(noticePath) as TextureImporter;
            Assert.NotNull(importer);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);
        }

        [Test]
        public void CardPrefabDragEventSourceRegistersTheDragHandlerCallbacks()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents("Assets/Resources/Ui/BattleCardItem.prefab");
            try
            {
                var view = prefabRoot.GetComponent<BattleCardItemView>();
                Assert.NotNull(view);
                Assert.NotNull(view.PreparationDragable);
                Assert.NotNull(view.CardDragListener);

                ((IUiInit)view.PreparationDragable).OnUiInit(null);
                Assert.NotNull(view.CardDragListener.OnPointerDown);
                Assert.NotNull(view.CardDragListener.OnPointerUp);
                Assert.NotNull(view.CardDragListener.OnDrag);
                Assert.IsNull(view.CardHoverListener.OnPointerDown);
                Assert.IsNull(view.CardClickListener.OnPointerDown);
                ((IUiDestroy)view.PreparationDragable).OnUiDestroy(null);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        [Test]
        public void LockedCollectionCardSuppressesFusionSealTextAndBuildsRecipeTooltip()
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(script);
            StringAssert.Contains("ShowCollectionEmptySlot(cardNumber);", script.text);
            StringAssert.Contains("ShowCollectionRecipeTooltip(cardNumber);", script.text);
            StringAssert.Contains("m_View.PreparationEmptyState.SetActive(true);", script.text);
            StringAssert.Contains("m_View.SkillDescriptionText.text = string.Empty", script.text);
        }

        [Test]
        public void LockedCollectionCardShowsConfiguredFusionRecipeOnHover()
        {
            var formatter = typeof(BattleCardItemController).GetMethod(
                "FormatCollectionRecipe",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(formatter);
            Assert.AreEqual(
                "合成配方：哥布林战士 + 哥布林战士",
                formatter.Invoke(null, new object[] { 100 }));
            Assert.AreEqual(
                "合成配方：无（基础卡牌）",
                formatter.Invoke(null, new object[] { 1 }));

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(script);
            StringAssert.Contains("ApplyInteractionPermissions(true, unlocked, false, true)", script.text);
            StringAssert.Contains("m_View.CardClickListener.enabled = clickEnabled", script.text);
            StringAssert.Contains("m_View.CardDragListener.enabled = dragEnabled", script.text);
        }

        [Test]
        public void LockedCollectionSlotUsesDedicatedNonBlockingPadlockOverlay()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            Assert.NotNull(view.CollectionLockedOverlay);
            Assert.NotNull(view.CollectionLockedOverlay.sprite);
            Assert.AreEqual("CardCollectionLockedPadlock", view.CollectionLockedOverlay.sprite.name);
            Assert.AreSame(view.PreparationEmptyState.transform, view.CollectionLockedOverlay.transform.parent);
            Assert.IsFalse(view.CollectionLockedOverlay.gameObject.activeSelf);
            Assert.IsFalse(view.CollectionLockedOverlay.raycastTarget);
            Assert.AreEqual(Vector2.zero, view.CollectionLockedOverlay.rectTransform.anchorMin);
            Assert.AreEqual(Vector2.one, view.CollectionLockedOverlay.rectTransform.anchorMax);
            Assert.AreEqual(new Vector2(8f, 8f), view.CollectionLockedOverlay.rectTransform.offsetMin);
            Assert.AreEqual(new Vector2(-8f, -8f), view.CollectionLockedOverlay.rectTransform.offsetMax);

            var importer = AssetImporter.GetAtPath(
                "Assets/Resources/Art/CardCollection/UI/CardCollectionLockedPadlock.png") as TextureImporter;
            Assert.NotNull(importer);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode);
            Assert.IsTrue(importer.alphaIsTransparency);
            Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode);

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Art/CardCollection/UI/CardCollectionLockedPadlock.png");
            Assert.NotNull(texture);
            Assert.AreEqual(1024, texture.width);
            Assert.AreEqual(1536, texture.height);

            var decodedTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.IsTrue(ImageConversion.LoadImage(
                    decodedTexture,
                    File.ReadAllBytes(
                        "Assets/Resources/Art/CardCollection/UI/CardCollectionLockedPadlock.png"),
                    false));
                var pixels = decodedTexture.GetPixels32();
                Assert.AreEqual(0, pixels[0].a);
                Assert.AreEqual(0, pixels[decodedTexture.width - 1].a);
                Assert.AreEqual(0, pixels[(decodedTexture.height - 1) * decodedTexture.width].a);
                Assert.AreEqual(0, pixels[pixels.Length - 1].a);
                Assert.Greater(pixels.Count(pixel => pixel.a == 0), pixels.Length / 2);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(decodedTexture);
            }

            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(script);
            StringAssert.Contains(
                "m_View.CollectionLockedOverlay.gameObject.SetActive(true);",
                script.text);
            StringAssert.Contains(
                "m_View.CollectionLockedOverlay.gameObject.SetActive(false);",
                script.text);
            Assert.AreEqual(
                1,
                script.text.Split(
                    new[] { "m_View.CollectionLockedOverlay.gameObject.SetActive(true);" },
                    StringSplitOptions.None).Length - 1);
        }

        [Test]
        public void FusionCollectionPreviewUsesSharedDeterministicSimulation()
        {
            var first = BattleCardSimulationFactory.CreateDeterministic(100);
            var second = BattleCardSimulationFactory.CreateDeterministic(100);
            Assert.AreEqual(first, second);
            Assert.AreEqual(100, first.CardNumber);
            Assert.AreEqual(EBattleCardTier.Silver, first.Tier);
            Assert.AreEqual(2, BattleKeywordRules.GetLevel(first.Keywords, EBattleKeyword.Taunt));
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
