using System;
using BbxCommon;
using NUnit.Framework;
using TMPro;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Random = Unity.Mathematics.Random;
using Resources = UnityEngine.Resources;

namespace Hearthstone.Tests
{
    public sealed class BattleRulesTests
    {
        [TearDown]
        public void TearDown()
        {
            DataApi.ReleaseAllData<BattleCardCsvData>(false);
            DataApi.ReleaseAllData<BattleCardTypeCsvData>(false);
        }

        [Test]
        public void BattleCardCsvTablesLoadTypeRangesAndNumberedCardPresentation()
        {
            const string typeCsv =
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack\n" +
                "// Unique card type identifier,Display name shown on cards,Minimum generated health inclusive,Maximum generated health inclusive,Minimum generated attack inclusive,Maximum generated attack inclusive\n" +
                "// Associated: BattleCardCsvData\n" +
                "1,哥布林战士,5,7,2,4\n";
            const string cardCsv =
                "CardNumber,CardTypeId,ArtworkKey\n" +
                "// Unique card number from 1 through 98,Card type identifier from BattleCardTypeCsvData,Artwork resource key\n" +
                "// Associated: BattleCardTypeCsvData\n" +
                "42,1,GoblinWarrior\n";

            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsv);
            CsvApi.ReadFromString<BattleCardCsvData>(nameof(BattleCardCsvData), cardCsv);

            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(1);
            Assert.NotNull(typeConfig);
            Assert.AreEqual("哥布林战士", typeConfig.DisplayName);
            Assert.AreEqual(5, typeConfig.MinHealth);
            Assert.AreEqual(7, typeConfig.MaxHealth);
            Assert.AreEqual(2, typeConfig.MinAttack);
            Assert.AreEqual(4, typeConfig.MaxAttack);
            Assert.AreEqual(EBattleCardTier.Bronze, typeConfig.Tier);

            var cardConfig = DataApi.GetData<BattleCardCsvData>(42);
            Assert.NotNull(cardConfig);
            Assert.AreEqual(42, cardConfig.CardNumber);
            Assert.AreEqual(1, cardConfig.CardTypeId);
            Assert.AreEqual("GoblinWarrior", cardConfig.ArtworkKey);
        }

        [Test]
        public void BattleCardTypeCsvLoadsAttackPresentationAndWaitsForItsVisualDuration()
        {
            const string typeCsv =
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack,InitialKeyword,AttackFrameAnimationKey,AttackAudioKeys,AttackAudioDelays,AttackAudioVolumes,HitDelays\n" +
                "// Unique card type identifier,Display name shown on cards,Minimum generated health inclusive,Maximum generated health inclusive,Minimum generated attack inclusive,Maximum generated attack inclusive,Initial battle keyword or None,Attack frame animation resource key,Attack sound resource keys,Seconds before attack sounds,Attack sound volumes,Seconds before red hit flashes\n" +
                "// Associated: BattleCardCsvData\n" +
                "1,哥布林战士,5,7,2,4,Taunt,BattleAttackSwordSlash,knifeSlice2;impactPunch_medium_002,0.12;0.30,0.72;0.58,0.18;0.34\n";

            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsv);

            var config = DataApi.GetData<BattleCardTypeCsvData>(1);
            Assert.NotNull(config);
            Assert.AreEqual("BattleAttackSwordSlash", config.AttackFrameAnimationKey);
            CollectionAssert.AreEqual(new[] { "knifeSlice2", "impactPunch_medium_002" }, config.AttackAudioKeys);
            CollectionAssert.AreEqual(new[] { 0.12f, 0.30f }, config.AttackAudioDelays);
            CollectionAssert.AreEqual(new[] { 0.72f, 0.58f }, config.AttackAudioVolumes);
            CollectionAssert.AreEqual(new[] { 0.18f, 0.34f }, config.HitDelays);
            Assert.That(
                BattleRules.GetAttackPresentationDuration(config),
                Is.EqualTo(0.34f + BattleRules.HitFlashDuration)
                    .Within(0.001f));
        }

        [Test]
        public void LegacyCardTypeConfigurationKeepsSafePresentationDefaults()
        {
            const string typeCsv =
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack\n" +
                "// Unique card type identifier,Display name shown on cards,Minimum generated health inclusive,Maximum generated health inclusive,Minimum generated attack inclusive,Maximum generated attack inclusive\n" +
                "// Associated: BattleCardCsvData\n" +
                "7,旧卡牌,2,2,1,1\n";

            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsv);

            var config = DataApi.GetData<BattleCardTypeCsvData>(7);
            Assert.NotNull(config);
            Assert.IsEmpty(config.AttackFrameAnimationKey);
            Assert.IsEmpty(config.AttackAudioKeys);
            Assert.IsEmpty(config.AttackAudioDelays);
            Assert.IsEmpty(config.AttackAudioVolumes);
            Assert.IsEmpty(config.HitDelays);
            Assert.That(
                BattleRules.GetAttackPresentationDuration(config),
                Is.EqualTo(BattleRules.AttackLungeDuration).Within(0.001f));
        }

        [Test]
        public void AttackPresentationWaitsForConfiguredAudioDelayWhenItIsLongest()
        {
            const string typeCsv =
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack,InitialKeyword,AttackFrameAnimationKey,AttackAudioKeys,AttackAudioDelays,AttackAudioVolumes,HitDelays\n" +
                "// Unique card type identifier,Display name shown on cards,Minimum generated health inclusive,Maximum generated health inclusive,Minimum generated attack inclusive,Maximum generated attack inclusive,Initial battle keyword or None,Attack frame animation resource key,Attack sound resource keys,Seconds before attack sounds,Attack sound volumes,Seconds before red hit flashes\n" +
                "// Associated: BattleCardCsvData\n" +
                "8,延迟音效测试,2,2,1,1,None,,earlyImpact;lateImpact,0.1;0.75,0.7;0.6,0.1\n";

            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsv);

            var config = DataApi.GetData<BattleCardTypeCsvData>(8);
            Assert.NotNull(config);
            Assert.That(
                BattleRules.GetAttackPresentationDuration(config),
                Is.EqualTo(config.AttackAudioDelays[1]).Within(0.001f));
        }

        [Test]
        public void AttackPresentationRejectsMismatchedAudioLists()
        {
            const string typeCsv =
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack,InitialKeyword,AttackFrameAnimationKey,AttackAudioKeys,AttackAudioDelays,AttackAudioVolumes,HitDelays\n" +
                "// Unique card type identifier,Display name shown on cards,Minimum generated health inclusive,Maximum generated health inclusive,Minimum generated attack inclusive,Maximum generated attack inclusive,Initial battle keyword or None,Attack frame animation resource key,Attack sound resource keys,Seconds before attack sounds,Attack sound volumes,Seconds before red hit flashes\n" +
                "// Associated: BattleCardCsvData\n" +
                "9,错误列表,2,2,1,1,None,,first;second,0.1,0.7;0.6,0.1\n";

            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsv);

            Assert.IsNull(DataApi.GetData<BattleCardTypeCsvData>(9));
        }

        [Test]
        public void RuntimeResourcesContainBattleCardCsvAndStageDataRegistry()
        {
            var registry = Resources.Load<ScriptableObjectAssets>(BbxVar.ExportScriptableObjectPathInResource);
            Assert.NotNull(registry, "GameStage returns before loading CSV data when this registry is missing.");
            Assert.NotNull(registry.Assets);

            ResourceApi.Initialize();
            var typeCsvAsset = ResourceApi.LoadTextAsset(nameof(BattleCardTypeCsvData));
            var cardCsvAsset = ResourceApi.LoadTextAsset(nameof(BattleCardCsvData));
            Assert.NotNull(typeCsvAsset);
            Assert.NotNull(cardCsvAsset);

            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsvAsset.text);
            CsvApi.ReadFromString<BattleCardCsvData>(nameof(BattleCardCsvData), cardCsvAsset.text);
            var cardConfig = DataApi.GetData<BattleCardCsvData>(BattleRules.DefaultCardNumber);
            Assert.NotNull(cardConfig);
            Assert.AreEqual(4, cardConfig.CardTypeId);
            Assert.AreEqual("Boar_001", cardConfig.ArtworkKey);

            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            Assert.NotNull(typeConfig);
            Assert.AreEqual("野猪", typeConfig.DisplayName);
            Assert.AreEqual(4, typeConfig.MinHealth);
            Assert.AreEqual(6, typeConfig.MaxHealth);

            Assert.NotNull(ResourceApi.LoadSprite("GoblinWarrior"));
            Assert.NotNull(ResourceApi.LoadSprite("GoblinArcher"));
            Assert.NotNull(ResourceApi.LoadSprite("GoblinBomber"));
            Assert.NotNull(ResourceApi.LoadSprite("Boar"));
            Assert.NotNull(ResourceApi.LoadSprite("Ogre"));
            Assert.NotNull(ResourceApi.LoadSprite("CardNumberBadgeHex"));
            Assert.NotNull(ResourceApi.LoadSprite("CardFrame-v3"));
            Assert.NotNull(ResourceApi.LoadSprite("BattleBoardBackground"));
            Assert.NotNull(ResourceApi.LoadSprite("BattleAttackSwordSlash"));
            Assert.NotNull(ResourceApi.LoadSprite("BattleAttackArrowImpact"));
            Assert.NotNull(ResourceApi.LoadSprite("BattleAttackSmallExplosion"));
            Assert.NotNull(ResourceApi.LoadSprite("BattleAttackSmallImpact"));
            Assert.NotNull(ResourceApi.LoadSprite("BattleAttackLargeImpact"));
            Assert.IsFalse(string.IsNullOrWhiteSpace(ResourceApi.GetFile("knifeSlice2").Path));
            Assert.IsFalse(string.IsNullOrWhiteSpace(ResourceApi.GetFile("impactWood_light_002").Path));
            Assert.IsFalse(string.IsNullOrWhiteSpace(ResourceApi.GetFile("explosionCrunch_001").Path));
            Assert.IsFalse(string.IsNullOrWhiteSpace(ResourceApi.GetFile("impactPunch_medium_002").Path));
            Assert.IsFalse(string.IsNullOrWhiteSpace(ResourceApi.GetFile("impactPunch_heavy_002").Path));
        }

        [Test]
        public void PreparationAndBattleUseSameSubtleParchmentAgingOverlay()
        {
            ResourceApi.Initialize();
            var sharedOverlay = ResourceApi.LoadSprite("ParchmentAgingOverlay");
            Assert.NotNull(sharedOverlay);

            var preparationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            var battlePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleView.prefab");
            Assert.NotNull(preparationPrefab);
            Assert.NotNull(battlePrefab);

            var preparationOverlay = preparationPrefab.transform
                .Find("ParchmentAgingOverlay")?.GetComponent<Image>();
            var battleOverlay = battlePrefab.transform
                .Find("ParchmentAgingOverlay")?.GetComponent<Image>();
            Assert.NotNull(preparationOverlay);
            Assert.NotNull(battleOverlay);
            Assert.AreSame(sharedOverlay, preparationOverlay.sprite);
            Assert.AreSame(sharedOverlay, battleOverlay.sprite);
            Assert.AreSame(preparationOverlay.sprite, battleOverlay.sprite);
            Assert.IsFalse(preparationOverlay.raycastTarget);
            Assert.IsFalse(battleOverlay.raycastTarget);
            Assert.That(preparationOverlay.color.a, Is.EqualTo(0.18f).Within(0.001f));
            Assert.That(battleOverlay.color.a, Is.EqualTo(0.14f).Within(0.001f));

            var preparationRect = (RectTransform)preparationOverlay.transform;
            var battleRect = (RectTransform)battleOverlay.transform;
            Assert.AreEqual(new Vector2(1700f, 380f), preparationRect.sizeDelta);
            Assert.AreEqual(new Vector2(0.055f, 0.07f), battleRect.anchorMin);
            Assert.AreEqual(new Vector2(0.945f, 0.93f), battleRect.anchorMax);
        }

        [Test]
        public void RandomPreparationRewardDealsFiveUniqueAvailableCardsFromOneThroughNinetyEight()
        {
            LoadRuntimeCardData();
            var runState = new RunStateSingletonRawComponent();
            for (var cardNumber = RunCardRules.FirstCardNumber; cardNumber <= 93; cardNumber++)
                runState.CardInstances[cardNumber] = new RunCardInstanceData(cardNumber, 1, 1);
            var random = new Random(12345u);

            var batch = PreparationRewardBatchFactory.CreateRandom(
                "random-reward-test",
                runState.HasCard,
                ref random);

            Assert.AreEqual(RunCardRules.RewardGrantCount, batch.Grants.Count);
            var dealt = new int[batch.Grants.Count];
            for (var index = 0; index < batch.Grants.Count; index++)
            {
                var grant = batch.Grants[index];
                dealt[index] = grant.CardNumber;
                Assert.That(grant.CardNumber, Is.InRange(94, RunCardRules.LastOrdinaryCardNumber));
                Assert.IsFalse(runState.HasCard(grant.CardNumber));

                var card = DataApi.GetData<BattleCardCsvData>(grant.CardNumber);
                var type = DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
                Assert.That(grant.Attack, Is.InRange(type.MinAttack, type.MaxAttack));
                Assert.That(grant.MaxHealth, Is.InRange(type.MinHealth, type.MaxHealth));
            }
            CollectionAssert.AreEquivalent(new[] { 94, 95, 96, 97, 98 }, dealt);
        }

        [Test]
        public void DefaultBattleRandomRewardExcludesInitialPlayerCards()
        {
            LoadRuntimeCardData();

            var startupData = BattleStageStartupData.CreateDefault(54321u);

            Assert.AreEqual(RunCardRules.RewardGrantCount, startupData.PreparationRewardBatch.Grants.Count);
            for (var index = 0; index < startupData.PreparationRewardBatch.Grants.Count; index++)
            {
                var cardNumber = startupData.PreparationRewardBatch.Grants[index].CardNumber;
                Assert.That(cardNumber, Is.InRange(
                    RunCardRules.FirstCardNumber,
                    RunCardRules.LastOrdinaryCardNumber));
                for (var slot = 0; slot < BattleRules.CardsPerSide; slot++)
                    Assert.AreNotEqual(BattleRules.GetCardNumber(EBattleSide.Player, slot), cardNumber);
            }
        }

        [Test]
        public void EngineDefersDefaultRandomRewardUntilGameEngineDataStageHasLoaded()
        {
            var engineScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Bootstrap/HearthstoneGameEngine.cs");
            Assert.NotNull(engineScript);
            var source = engineScript.text;
            var onAwakeStart = source.IndexOf("protected override void OnAwake()", StringComparison.Ordinal);
            var enterBattleStart = source.IndexOf(
                "public void EnterBattleStageGroup",
                onAwakeStart,
                StringComparison.Ordinal);
            var loadingCompletedStart = source.IndexOf(
                "protected override void OnStageLoadingCompleted",
                enterBattleStart,
                StringComparison.Ordinal);
            var submitStart = source.IndexOf(
                "private void TrySubmitRequestedStageGroup",
                loadingCompletedStart,
                StringComparison.Ordinal);
            Assert.That(onAwakeStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(enterBattleStart, Is.GreaterThan(onAwakeStart));
            Assert.That(loadingCompletedStart, Is.GreaterThan(enterBattleStart));
            Assert.That(submitStart, Is.GreaterThan(loadingCompletedStart));

            var onAwakeSource = source.Substring(onAwakeStart, enterBattleStart - onAwakeStart);
            var loadingCompletedSource = source.Substring(
                loadingCompletedStart,
                submitStart - loadingCompletedStart);
            StringAssert.DoesNotContain("BattleStageStartupData.CreateDefault()", onAwakeSource);
            StringAssert.Contains("BattleStageStartupData.CreateDefault()", loadingCompletedSource);
            StringAssert.Contains(
                "m_RequestedBattleStartupData == null && m_RequestedPreparationBatch == null",
                loadingCompletedSource);
        }

        [Test]
        public void BattleFontContainsCurrentChineseInterfaceCharacters()
        {
            var font = Resources.Load<TMP_FontAsset>("Fonts/NotoSansSC-Dynamic SDF");
            Assert.NotNull(font);
            const string interfaceCharacters = "战斗进行中胜利失败阵亡哥布林战士弓手投弹野猪食人魔";
            if (font.HasCharacters(interfaceCharacters) == false)
            {
                Assert.IsTrue(font.TryAddCharacters(interfaceCharacters, out var missingCharacters));
                Assert.IsTrue(string.IsNullOrEmpty(missingCharacters));
            }
            Assert.IsTrue(font.HasCharacters(interfaceCharacters));
        }

        [Test]
        public void BattleCardPrefabRaisesFrameBottomAndRendersStatsAboveIt()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);

            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            Assert.NotNull(view.CardFrame);
            Assert.NotNull(view.AttackerHighlight);
            Assert.NotNull(view.TargetHighlight);
            Assert.AreEqual("CardFrame-v3", view.CardFrame.sprite.name);
            Assert.AreSame(view.CardFrame.sprite, view.AttackerHighlight.sprite);
            Assert.AreSame(view.CardFrame.sprite, view.TargetHighlight.sprite);

            AssertFrameBottomIsRaised(view.CardFrame.rectTransform);
            AssertFrameBottomIsRaised(view.AttackerHighlight.rectTransform);
            AssertFrameBottomIsRaised(view.TargetHighlight.rectTransform);
            AssertFramesRenderBelowCardMarkers(view);
            Assert.NotNull(view.ArtworkArea);
            Assert.AreEqual(Image.Type.Simple, view.ArtworkArea.type);
            Assert.IsFalse(view.ArtworkArea.preserveAspect);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), view.ArtworkArea.rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), view.ArtworkArea.rectTransform.anchorMax);
            Assert.AreEqual(new Vector2(210f, 297f), view.ArtworkArea.rectTransform.sizeDelta);
        }

        [Test]
        public void BattleCardArtworkNormalizesImportedRatiosToSlightlyWideDrawingRect()
        {
            var ordinaryTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Art/BattleCards/GoblinWarrior_004.png");
            var fusionTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Resources/Art/BattleCards/FusionCard_099.png");
            Assert.NotNull(ordinaryTexture);
            Assert.NotNull(fusionTexture);
            Assert.AreEqual(new Vector2Int(1024, 2048), new Vector2Int(
                ordinaryTexture.width,
                ordinaryTexture.height));
            Assert.AreEqual(new Vector2Int(1024, 1536), new Vector2Int(
                fusionTexture.width,
                fusionTexture.height));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);
            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            Assert.IsFalse(view.ArtworkArea.preserveAspect);
            Assert.AreEqual(new Vector2(210f, 297f), view.ArtworkArea.rectTransform.sizeDelta);
            Assert.AreEqual(210f / 297f, view.ArtworkArea.rectTransform.sizeDelta.x /
                view.ArtworkArea.rectTransform.sizeDelta.y, 0.0001f);
            Assert.Greater(210f / 297f, 2f / 3f);
            Assert.AreEqual(20f, (250f - view.ArtworkArea.rectTransform.sizeDelta.x) * 0.5f);

            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains("m_View.ArtworkArea.preserveAspect = false", controllerScript.text);
        }

        [Test]
        public void BattleCardHoverUsesUnifiedFramePaletteAndPreparationOnlyInteraction()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);

            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            Assert.NotNull(view.CardHoverListener);
            Assert.NotNull(view.CardHoverInput);
            Assert.AreSame(view.CardHoverListener, view.PreparationDragable.EventListener);
            var hoverImage = view.CardHoverInput;
            Assert.NotNull(hoverImage);
            Assert.AreSame(view.CardBackground, hoverImage);
            Assert.IsFalse(view.CardHoverListener.enabled);
            Assert.IsFalse(hoverImage.raycastTarget);
            Assert.IsFalse(view.PreparationDragable.enabled);
            Assert.IsFalse(view.PreparationDragable.EventListener.enabled);
            Assert.IsFalse(view.PreparationInteractor.enabled);

            AssertColor32(BattleCardItemController.BronzeFrameColor, 184, 115, 51, 255);
            AssertColor32(BattleCardItemController.SilverFrameColor, 192, 204, 216, 255);
            AssertColor32(BattleCardItemController.GoldFrameColor, 231, 169, 59, 255);
            AssertColor32(BattleCardItemController.LegendaryFrameColor, 178, 92, 255, 255);
            AssertColor32(BattleCardItemController.HoverFrameColor, 255, 210, 48, 255);
            Assert.AreEqual(
                BattleCardItemController.LegendaryFrameColor,
                BattleCardItemController.GetTierFrameColor(EBattleCardTier.Legendary));

            const string emptySlotPath =
                "Assets/Resources/Art/Preparation/UI/PreparationPoolEmptySlot.png";
            var emptySlotTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(emptySlotPath);
            var emptySlotSprite = AssetDatabase.LoadAssetAtPath<Sprite>(emptySlotPath);
            var emptySlotImporter = AssetImporter.GetAtPath(emptySlotPath) as TextureImporter;
            var emptySlotImage = view.PreparationEmptyState.GetComponent<Image>();
            Assert.NotNull(emptySlotTexture);
            Assert.NotNull(emptySlotSprite);
            Assert.NotNull(emptySlotImporter);
            Assert.NotNull(emptySlotImage);
            Assert.AreEqual(1024, emptySlotTexture.width);
            Assert.AreEqual(1536, emptySlotTexture.height);
            Assert.IsTrue(emptySlotImporter.DoesSourceTextureHaveAlpha());
            Assert.AreSame(emptySlotSprite, emptySlotImage.sprite);
            Assert.IsTrue(emptySlotImage.preserveAspect);
            var emptySlotRect = (RectTransform)view.PreparationEmptyState.transform;
            Assert.AreEqual(Vector2.zero, emptySlotRect.offsetMin);
            Assert.AreEqual(Vector2.zero, emptySlotRect.offsetMax);

            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains("SetHoverEnabled(preparationMode && (occupied || m_PreparationCardLocked))", controllerScript.text);
            StringAssert.Contains("var dragEnabled = preparationMode && occupied", controllerScript.text);
            StringAssert.Contains("PreparationEmptyAttemptListener.enabled = false", controllerScript.text);
            StringAssert.Contains("emptyInput.raycastTarget = false", controllerScript.text);
            StringAssert.Contains("HidePreparationEmptyStates();", controllerScript.text);
            StringAssert.Contains("m_View.CardBackground.color = Color.clear", controllerScript.text);
            StringAssert.Contains("m_PreparationPage?.ForwardCardPoolScroll(eventData)", controllerScript.text);
            StringAssert.Contains("var restoredSlotPosition = m_View.transform.localPosition", controllerScript.text);
            StringAssert.Contains("transformSetter?.PosWrapper.SetLocalPositionOnce", controllerScript.text);
            StringAssert.DoesNotContain("m_View.transform.localPosition = Vector3.zero", controllerScript.text);
            StringAssert.DoesNotContain("emptyFrame.sprite = ResourceApi.LoadSprite", controllerScript.text);
            StringAssert.DoesNotContain("CardFrameBlue-v2", controllerScript.text);
            StringAssert.DoesNotContain("var frameColor = m_Card.Side", controllerScript.text);
            StringAssert.Contains("GetTierFrameColor(m_Card.Tier)", controllerScript.text);
            StringAssert.Contains("GetTierFrameColor(instance.Tier)", controllerScript.text);
        }

        [Test]
        public void BattleCardPreparationHoverAndDragShareOneUnblockedInputSurface()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);

            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            Assert.NotNull(view.CardBackground);
            Assert.NotNull(view.CardHoverInput);
            Assert.NotNull(view.CardHoverListener);
            Assert.NotNull(view.PreparationDragable);
            Assert.NotNull(view.PreparationDragable.EventListener);
            Assert.AreSame(view.CardBackground, view.CardHoverInput);
            Assert.AreSame(view.CardHoverListener, view.PreparationDragable.EventListener);
            Assert.AreSame(prefab, view.CardHoverListener.gameObject);
            Assert.IsNull(prefab.transform.Find("HoverInput"));
        }

        [Test]
        public void BattleCardSkillBaseUsesSparseLightPatternBehindText()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);

            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);
            var skillArea = view.SkillDescriptionText.transform.parent;
            var patternTransform = skillArea.Find("CardBasePattern");
            Assert.NotNull(patternTransform);
            Assert.AreEqual(0, patternTransform.GetSiblingIndex());

            var pattern = patternTransform.GetComponent<TextMeshProUGUI>();
            Assert.NotNull(pattern);
            Assert.AreEqual("◇  ·        ·  ◇\n  ∽          ∽", pattern.text);
            Assert.AreEqual(TextAlignmentOptions.Center, pattern.alignment);
            Assert.IsFalse(pattern.enableWordWrapping);
            Assert.IsFalse(pattern.raycastTarget);
            Assert.That(pattern.color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(pattern.color.g, Is.EqualTo(0.9f).Within(0.001f));
            Assert.That(pattern.color.b, Is.EqualTo(0.68f).Within(0.001f));
            Assert.That(pattern.color.a, Is.EqualTo(0.12f).Within(0.001f));
            Assert.NotNull(pattern.font.sourceFontFile);
            Assert.IsTrue(pattern.font.sourceFontFile.HasCharacter('◇'));
            Assert.IsTrue(pattern.font.sourceFontFile.HasCharacter('·'));
            Assert.IsTrue(pattern.font.sourceFontFile.HasCharacter('∽'));
        }

        [Test]
        public void PreparationPoolAndSlotsUseSharedBattleCardAndMatchItsAspectRatio()
        {
            var sharedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(sharedPrefab);
            var sharedView = sharedPrefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(sharedView);
            Assert.NotNull(sharedView.PreparationEmptyState);
            Assert.NotNull(sharedView.PreparationBattleSlotEmptyState);
            Assert.NotNull(sharedView.PreparationFusionSlotEmptyState);
            Assert.NotNull(sharedView.PreparationMaterialSelectedState);
            Assert.NotNull(sharedView.PreparationDropHighlight);
            Assert.NotNull(sharedView.PreparationDragable);
            Assert.NotNull(sharedView.PreparationInteractor);
            Assert.NotNull(sharedView.PreparationEmptyAttemptListener);
            var sharedSize = ((RectTransform)sharedPrefab.transform).sizeDelta;
            var sharedAspect = sharedSize.x / sharedSize.y;

            var preparationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            Assert.NotNull(preparationPrefab);
            var preparationView = preparationPrefab.GetComponent<PreparationView>();
            Assert.NotNull(preparationView);
            var poolSlotSize = preparationView.CardPoolList.ConstantSlotSize;
            Assert.AreEqual(sharedAspect, poolSlotSize.x / poolSlotSize.y, 0.0001f);
            var battleSlotSize = preparationView.BattleSlotList.ConstantSlotSize;
            Assert.AreEqual(sharedAspect, battleSlotSize.x / battleSlotSize.y, 0.0001f);
            var fusionSlotSize = preparationView.FusionSlotList.ConstantSlotSize;
            Assert.AreEqual(sharedAspect, fusionSlotSize.x / fusionSlotSize.y, 0.0001f);

            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/PreparationController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains(
                "BattleSlotList.ItemWrapper.AddItem<BattleCardItemController>()",
                controllerScript.text);
            StringAssert.Contains(
                "FusionSlotList.ItemWrapper.AddItem<BattleCardItemController>()",
                controllerScript.text);
            StringAssert.DoesNotContain("AddItem<PreparationSlotItemController>()", controllerScript.text);
            StringAssert.DoesNotContain("AddItem<PreparationFusionSlotItemController>()", controllerScript.text);
            StringAssert.Contains("CardPoolScrollRect.scrollSensitivity *= 1.5f", controllerScript.text);
            StringAssert.DoesNotContain("OnCardPoolScrollChanged", controllerScript.text);
            StringAssert.DoesNotContain("DebugApi.Log(", controllerScript.text);
            Assert.IsTrue(
                typeof(UnityEngine.EventSystems.IScrollHandler).IsAssignableFrom(typeof(BattleCardItemController)));
        }

        [Test]
        public void BattleCardStatsUseReadableSwappedBadges()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(prefab);

            var view = prefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(view);

            var healthBadge = view.HealthText.transform.parent.GetComponent<Image>();
            var attackBadge = view.AttackText.transform.parent.GetComponent<Image>();
            Assert.NotNull(healthBadge);
            Assert.NotNull(attackBadge);
            Assert.AreEqual(Vector2.zero, healthBadge.rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(30f, 30f), healthBadge.rectTransform.anchoredPosition);
            Assert.AreEqual(new Vector2(1f, 0f), attackBadge.rectTransform.anchorMin);
            Assert.AreEqual(new Vector2(-30f, 30f), attackBadge.rectTransform.anchoredPosition);
            Assert.AreEqual("HealthDropBadge", healthBadge.sprite.name);
            Assert.AreEqual("AttackBadgeFrame", attackBadge.sprite.name);

            AssertReadableStatText(view.HealthText);
            AssertReadableStatText(view.AttackText);
        }

        [Test]
        public void BattleCardRuntimeStateUsesConfigurationAndResetsWhenCollected()
        {
            var cardConfig = new BattleCardCsvData
            {
                CardNumber = 7,
                CardTypeId = 3,
                ArtworkKey = "TestArtwork",
            };
            var typeConfig = new BattleCardTypeCsvData
            {
                CardTypeId = 3,
                DisplayName = "Test card",
                MinAttack = 4,
                MaxAttack = 6,
                MinHealth = 8,
                MaxHealth = 10,
                Tier = EBattleCardTier.Gold,
            };
            var card = new BattleCardRawComponent();
            var random = new Random(12345u);

            card.Initialize(EBattleSide.Enemy, 2, cardConfig, typeConfig, ref random);

            Assert.AreEqual(7, card.CardNumber);
            Assert.AreEqual(3, card.CardTypeId);
            Assert.AreEqual(EBattleSide.Enemy, card.Side);
            Assert.AreEqual(2, card.SlotIndex);
            Assert.AreEqual(EBattleCardTier.Gold, card.Tier);
            Assert.That(card.Attack, Is.InRange(4, 6));
            Assert.AreEqual(card.Attack, card.EntryAttack);
            Assert.That(card.MaxHealth, Is.InRange(8, 10));
            Assert.AreEqual(card.MaxHealth, card.CurrentHealth.Value);
            Assert.AreEqual(card.CurrentHealth.Value, card.EntryHealth);
            Assert.IsTrue(card.IsAlive.Value);

            var entryAttack = card.EntryAttack;
            var entryHealth = card.EntryHealth;
            card.SetAttack(entryAttack + 2);
            card.SetCurrentHealthWithoutAliveCommit(entryHealth - 1);
            Assert.AreEqual(entryAttack, card.EntryAttack);
            Assert.AreEqual(entryHealth, card.EntryHealth);

            card.CollectToPool();

            Assert.AreEqual(0, card.CardNumber);
            Assert.AreEqual(0, card.CardTypeId);
            Assert.AreEqual(0, card.Attack);
            Assert.AreEqual(0, card.EntryAttack);
            Assert.AreEqual(EBattleCardTier.Bronze, card.Tier);
            Assert.AreEqual(0, card.MaxHealth);
            Assert.AreEqual(0, card.EntryHealth);
            Assert.AreEqual(0, card.CurrentHealth.Value);
            Assert.IsFalse(card.IsAlive.Value);

            DataApi.SetData(cardConfig.CardNumber, cardConfig);
            var legendaryInstance = new RunCardInstanceData(
                cardConfig.CardNumber,
                9,
                12,
                EBattleKeyword.None,
                EBattleCardTier.Legendary);
            card.InitializePlayer(1, legendaryInstance);
            Assert.AreEqual(EBattleCardTier.Legendary, card.Tier);
            Assert.AreEqual(EBattleSide.Player, card.Side);

            card.InitializePlayerExplicit(1, legendaryInstance, 11, 14, 8);
            Assert.AreEqual(11, card.EntryAttack);
            Assert.AreEqual(8, card.EntryHealth);
            Assert.AreEqual(11, card.Attack);
            Assert.AreEqual(14, card.MaxHealth);
            Assert.AreEqual(8, card.CurrentHealth.Value);
        }

        [Test]
        public void BattleCardStatTextColorComparesAgainstEntryValue()
        {
            AssertColor32(BattleCardItemController.LowerStatTextColor, 255, 92, 92, 255);
            AssertColor32(BattleCardItemController.DefaultStatTextColor, 255, 255, 255, 255);
            AssertColor32(BattleCardItemController.HigherStatTextColor, 88, 176, 255, 255);

            Assert.AreEqual(
                BattleCardItemController.LowerStatTextColor,
                BattleCardItemController.GetStatTextColor(4, 5));
            Assert.AreEqual(
                BattleCardItemController.DefaultStatTextColor,
                BattleCardItemController.GetStatTextColor(5, 5));
            Assert.AreEqual(
                BattleCardItemController.HigherStatTextColor,
                BattleCardItemController.GetStatTextColor(6, 5));

            var controllerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
                "Assets/Scripts/Hearthstone/Ui/Controller/BattleCardItemController.cs");
            Assert.NotNull(controllerScript);
            StringAssert.Contains("GetStatTextColor(health, m_Card.EntryHealth)", controllerScript.text);
            StringAssert.Contains("GetStatTextColor(attack, m_Card.EntryAttack)", controllerScript.text);
            StringAssert.Contains("m_Card == null", controllerScript.text);
        }

        [Test]
        public void NumberedCardTableContainsContinuousBalancedAssignmentsWithBiasedOgres()
        {
            ResourceApi.Initialize();
            var cardCsvAsset = ResourceApi.LoadTextAsset(nameof(BattleCardCsvData));
            var typeCsvAsset = ResourceApi.LoadTextAsset(nameof(BattleCardTypeCsvData));
            Assert.NotNull(cardCsvAsset);
            Assert.NotNull(typeCsvAsset);
            CsvApi.ReadFromString<BattleCardCsvData>(nameof(BattleCardCsvData), cardCsvAsset.text);
            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsvAsset.text);

            var counts = new int[6];
            var numberSums = new int[6];
            for (var cardNumber = 1; cardNumber <= 98; cardNumber++)
            {
                var config = DataApi.GetData<BattleCardCsvData>(cardNumber);
                Assert.NotNull(config, $"Card number {cardNumber} is missing.");
                Assert.AreEqual(cardNumber, config.CardNumber);
                Assert.That(config.CardTypeId, Is.InRange(1, 5));
                Assert.IsFalse(string.IsNullOrWhiteSpace(config.ArtworkKey));
                counts[config.CardTypeId]++;
                numberSums[config.CardTypeId] += cardNumber;
                if (config.CardTypeId == 5)
                    Assert.That(cardNumber, Is.InRange(35, 98));
            }

            CollectionAssert.AreEqual(new[] { 0, 20, 20, 20, 19, 19 }, counts);
            Assert.Greater(numberSums[5] / (double)counts[5], 70d);
            Assert.AreEqual(5, DataApi.GetData<BattleCardCsvData>(40).CardTypeId);
            Assert.AreNotEqual(5, DataApi.GetData<BattleCardCsvData>(93).CardTypeId);
            Assert.AreNotEqual(5, DataApi.GetData<BattleCardCsvData>(98).CardTypeId);
            var lockedCard = DataApi.GetData<BattleCardCsvData>(RunCardRules.LockedCardNumber);
            Assert.NotNull(lockedCard);
            Assert.AreEqual(99, lockedCard.CardTypeId);
            Assert.AreEqual("FusionCard_099", lockedCard.ArtworkKey);
            Assert.NotNull(ResourceApi.LoadSprite("FusionCard_099"));

            var fusionCard = DataApi.GetData<BattleCardCsvData>(RunCardRules.FirstFusionCardNumber);
            Assert.NotNull(fusionCard);
            Assert.AreEqual(RunCardRules.FirstFusionCardNumber, fusionCard.CardTypeId);
            Assert.AreEqual("FusionCard_100", fusionCard.ArtworkKey);
            Assert.NotNull(ResourceApi.LoadSprite(fusionCard.ArtworkKey));
            CollectionAssert.AreEqual(new[] { 1, 1 }, fusionCard.FusionRecipeTypeIds);

            for (var cardNumber = RunCardRules.FirstFusionCardNumber;
                 cardNumber < RunCardRules.FirstLegendaryCardNumber;
                 cardNumber++)
            {
                var fusionArtworkConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
                Assert.NotNull(fusionArtworkConfig, $"Fusion card {cardNumber} is missing.");
                Assert.AreEqual($"FusionCard_{cardNumber}", fusionArtworkConfig.ArtworkKey);
                Assert.NotNull(ResourceApi.LoadSprite(fusionArtworkConfig.ArtworkKey),
                    fusionArtworkConfig.ArtworkKey);
            }

            var fusionType = DataApi.GetData<BattleCardTypeCsvData>(RunCardRules.FirstFusionCardNumber);
            Assert.NotNull(fusionType);
            Assert.AreEqual("武士", fusionType.DisplayName);
            Assert.AreEqual(0, fusionType.MinHealth);
            Assert.AreEqual(0, fusionType.MaxHealth);
            Assert.AreEqual(0, fusionType.MinAttack);
            Assert.AreEqual(0, fusionType.MaxAttack);
            Assert.AreEqual(EBattleCardTier.Silver, fusionType.Tier);

            var goldType = DataApi.GetData<BattleCardTypeCsvData>(115);
            var legendaryCard = DataApi.GetData<BattleCardCsvData>(RunCardRules.FirstLegendaryCardNumber);
            var legendaryType = DataApi.GetData<BattleCardTypeCsvData>(RunCardRules.FirstLegendaryCardNumber);
            Assert.NotNull(goldType);
            Assert.NotNull(legendaryCard);
            Assert.NotNull(legendaryType);
            Assert.AreEqual(EBattleCardTier.Gold, goldType.Tier);
            Assert.AreEqual(EBattleCardTier.Legendary, legendaryType.Tier);
            CollectionAssert.AreEqual(new[] { 1, 1, 1, 1 }, legendaryCard.FusionRecipeTypeIds);

            var recipeCounts = new int[RunCardRules.FusionSlotCount + 1];
            for (var cardNumber = RunCardRules.FirstFusionCardNumber;
                 cardNumber <= RunCardRules.LastFusionCardNumber;
                 cardNumber++)
            {
                var card = DataApi.GetData<BattleCardCsvData>(cardNumber);
                var type = card == null ? null : DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
                Assert.NotNull(card, $"Fusion card {cardNumber} is missing.");
                Assert.NotNull(type, $"Fusion type {cardNumber} is missing.");
                var materialCount = card.FusionRecipeTypeIds.Count;
                recipeCounts[materialCount]++;
                Assert.AreEqual(RunCardRules.GetTierForFusionMaterialCount(materialCount), type.Tier);
            }
            Assert.AreEqual(15, recipeCounts[2]);
            Assert.AreEqual(34, recipeCounts[3]);
            Assert.AreEqual(65, recipeCounts[4]);

            var otherAverageHealth = 0d;
            var otherAverageAttack = 0d;
            for (var typeId = 1; typeId <= 4; typeId++)
            {
                var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(typeId);
                otherAverageHealth += (typeConfig.MinHealth + typeConfig.MaxHealth) * 0.5d;
                otherAverageAttack += (typeConfig.MinAttack + typeConfig.MaxAttack) * 0.5d;
            }
            otherAverageHealth /= 4d;
            otherAverageAttack /= 4d;

            var ogreType = DataApi.GetData<BattleCardTypeCsvData>(5);
            var ogreAverageHealth = (ogreType.MinHealth + ogreType.MaxHealth) * 0.5d;
            var ogreAverageAttack = (ogreType.MinAttack + ogreType.MaxAttack) * 0.5d;
            Assert.That(ogreAverageHealth / otherAverageHealth, Is.EqualTo(1.5d).Within(0.1d));
            Assert.That(ogreAverageAttack / otherAverageAttack, Is.EqualTo(1.5d).Within(0.01d));
        }

        [Test]
        public void InitialLineupContainsAllFiveCardTypes()
        {
            ResourceApi.Initialize();
            var cardCsvAsset = ResourceApi.LoadTextAsset(nameof(BattleCardCsvData));
            Assert.NotNull(cardCsvAsset);
            CsvApi.ReadFromString<BattleCardCsvData>(nameof(BattleCardCsvData), cardCsvAsset.text);

            var lineupTypes = new System.Collections.Generic.HashSet<int>();
            for (var slot = 0; slot < BattleRules.CardsPerSide; slot++)
            {
                var playerNumber = BattleRules.GetCardNumber(EBattleSide.Player, slot);
                var enemyNumber = BattleRules.GetCardNumber(EBattleSide.Enemy, slot);
                Assert.That(playerNumber, Is.InRange(RunCardRules.FirstCardNumber, RunCardRules.LastOrdinaryCardNumber));
                Assert.That(enemyNumber, Is.InRange(RunCardRules.FirstCardNumber, RunCardRules.LastOrdinaryCardNumber));
                var playerType = DataApi.GetData<BattleCardCsvData>(playerNumber).CardTypeId;
                var enemyType = DataApi.GetData<BattleCardCsvData>(enemyNumber).CardTypeId;
                Assert.That(playerType, Is.InRange(1, 5));
                Assert.That(enemyType, Is.InRange(1, 5));
                lineupTypes.Add(playerType);
                lineupTypes.Add(enemyType);
            }

            CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4, 5 }, lineupTypes);
            Assert.AreEqual(4, BattleRules.GetCardNumber(EBattleSide.Player, 0));
            Assert.AreEqual(1, BattleRules.GetCardNumber(EBattleSide.Player, 1));
            Assert.AreEqual(40, BattleRules.GetCardNumber(EBattleSide.Player, 2));
            Assert.AreEqual(5, BattleRules.GetCardNumber(EBattleSide.Enemy, 0));
            Assert.AreEqual(2, BattleRules.GetCardNumber(EBattleSide.Enemy, 1));
            Assert.AreEqual(9, BattleRules.GetCardNumber(EBattleSide.Enemy, 2));
        }

        [Test]
        public void LivingAttackerSequenceWrapsFromLeftToRight()
        {
            var cursor = 0;
            var expectedSlots = new[] { 0, 1, 2, 0 };

            foreach (var expectedSlot in expectedSlots)
            {
                var actualSlot = BattleRules.FindNextLivingSlot(cursor, BattleRules.InitialAliveMask);
                Assert.AreEqual(expectedSlot, actualSlot);
                cursor = BattleRules.GetNextCursor(actualSlot);
            }
        }

        [Test]
        public void DeadCardsAreSkippedWithoutChangingSlots()
        {
            const uint aliveMask = (1u << 0) | (1u << 2);

            Assert.AreEqual(2, BattleRules.FindNextLivingSlot(1, aliveMask));
            Assert.AreEqual(0, BattleRules.FindNextLivingSlot(0, aliveMask));
        }

        [Test]
        public void FiveHealthThreeAttackLeavesBothCardsAtTwoHealth()
        {
            BattleRules.ResolveSimultaneousDamage(5, 3, 5, 3, out var attackerHealth, out var targetHealth);

            Assert.AreEqual(2, attackerHealth);
            Assert.AreEqual(2, targetHealth);
        }

        [Test]
        public void TargetSelectionOnlyReturnsLivingSlotsAndIsSeedReproducible()
        {
            const uint aliveMask = (1u << 0) | (1u << 2);
            var firstRandom = new Random(12345u);
            var secondRandom = new Random(12345u);

            for (var index = 0; index < 16; index++)
            {
                var firstSlot = BattleRules.SelectLivingSlot(
                    aliveMask,
                    firstRandom.NextInt(BattleRules.CountLiving(aliveMask)));
                var secondSlot = BattleRules.SelectLivingSlot(
                    aliveMask,
                    secondRandom.NextInt(BattleRules.CountLiving(aliveMask)));
                Assert.That(firstSlot, Is.EqualTo(0).Or.EqualTo(2));
                Assert.AreEqual(firstSlot, secondSlot);
            }
        }

        [Test]
        public void BothSidesEmptyIsPlayerVictory()
        {
            Assert.AreEqual(EBattleResult.PlayerVictory, BattleRules.EvaluateResult(0u, 0u));
        }

        [Test]
        public void PlayerEmptyWhileEnemyLivesIsEnemyVictory()
        {
            Assert.AreEqual(EBattleResult.EnemyVictory, BattleRules.EvaluateResult(0u, 1u));
        }

        [Test]
        public void BattleStopsAfterAResultIsFinal()
        {
            Assert.IsTrue(BattleRules.CanAct(EBattleResult.InProgress));
            Assert.IsFalse(BattleRules.CanAct(EBattleResult.PlayerVictory));
            Assert.IsFalse(BattleRules.CanAct(EBattleResult.EnemyVictory));
        }

        [Test]
        public void SidesAlternateDeterministically()
        {
            var side = EBattleSide.Player;
            var expected = new[]
            {
                EBattleSide.Enemy,
                EBattleSide.Player,
                EBattleSide.Enemy,
                EBattleSide.Player,
            };

            foreach (var expectedSide in expected)
            {
                side = BattleRules.GetOppositeSide(side);
                Assert.AreEqual(expectedSide, side);
            }
        }

        private static void LoadRuntimeCardData()
        {
            ResourceApi.Initialize();
            var typeCsvAsset = ResourceApi.LoadTextAsset(nameof(BattleCardTypeCsvData));
            var cardCsvAsset = ResourceApi.LoadTextAsset(nameof(BattleCardCsvData));
            Assert.NotNull(typeCsvAsset);
            Assert.NotNull(cardCsvAsset);
            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsvAsset.text);
            CsvApi.ReadFromString<BattleCardCsvData>(nameof(BattleCardCsvData), cardCsvAsset.text);
        }

        private static void AssertFrameBottomIsRaised(RectTransform frame)
        {
            Assert.AreEqual(Vector2.zero, frame.anchorMin);
            Assert.AreEqual(Vector2.one, frame.anchorMax);
            Assert.AreEqual(new Vector2(0f, 24f), frame.offsetMin);
            Assert.AreEqual(Vector2.zero, frame.offsetMax);
        }

        private static void AssertFramesRenderBelowCardMarkers(BattleCardItemView view)
        {
            var healthBadge = view.HealthText.transform.parent;
            var attackBadge = view.AttackText.transform.parent;
            var cardNumberBadge = view.CardNumberBadge.transform;
            var frames = new[]
            {
                view.CardFrame.transform,
                view.AttackerHighlight.transform,
                view.TargetHighlight.transform,
            };

            foreach (var frame in frames)
            {
                Assert.Less(frame.GetSiblingIndex(), healthBadge.GetSiblingIndex());
                Assert.Less(frame.GetSiblingIndex(), attackBadge.GetSiblingIndex());
                Assert.Less(frame.GetSiblingIndex(), cardNumberBadge.GetSiblingIndex());
            }
        }

        private static void AssertReadableStatText(TMP_Text text)
        {
            Assert.GreaterOrEqual(text.fontSize, 30f);
            Assert.IsTrue((text.fontStyle & FontStyles.Bold) != 0);
            Assert.AreEqual(Color.white, text.color);
            var outline = text.GetComponent<Outline>();
            Assert.NotNull(outline);
            Assert.Greater(outline.effectColor.a, 0.9f);
        }

        private static void AssertColor32(Color color, byte red, byte green, byte blue, byte alpha)
        {
            var actual = (Color32)color;
            Assert.AreEqual(red, actual.r);
            Assert.AreEqual(green, actual.g);
            Assert.AreEqual(blue, actual.b);
            Assert.AreEqual(alpha, actual.a);
        }
    }
}
