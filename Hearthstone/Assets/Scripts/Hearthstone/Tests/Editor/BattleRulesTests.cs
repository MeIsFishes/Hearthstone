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

            var cardConfig = DataApi.GetData<BattleCardCsvData>(42);
            Assert.NotNull(cardConfig);
            Assert.AreEqual(42, cardConfig.CardNumber);
            Assert.AreEqual(1, cardConfig.CardTypeId);
            Assert.AreEqual("GoblinWarrior", cardConfig.ArtworkKey);
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
            Assert.AreEqual("Boar", cardConfig.ArtworkKey);

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
            Assert.NotNull(ResourceApi.LoadSprite("CardFrameBlue-v2"));
            Assert.NotNull(ResourceApi.LoadSprite("BattleBoardBackground"));
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
        public void BattleCardPrefabUsesFullCardFramesForEveryCombatState()
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

            AssertFrameCoversEntireCard(view.CardFrame.rectTransform);
            AssertFrameCoversEntireCard(view.AttackerHighlight.rectTransform);
            AssertFrameCoversEntireCard(view.TargetHighlight.rectTransform);
            AssertFramesRenderBelowCardMarkers(view);
            Assert.NotNull(view.ArtworkArea);
            Assert.AreEqual(Image.Type.Simple, view.ArtworkArea.type);
            Assert.IsTrue(view.ArtworkArea.preserveAspect);
        }

        [Test]
        public void PreparationPoolUsesSharedBattleCardAndSlotsMatchItsAspectRatio()
        {
            var sharedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/BattleCardItem.prefab");
            Assert.NotNull(sharedPrefab);
            var sharedView = sharedPrefab.GetComponent<BattleCardItemView>();
            Assert.NotNull(sharedView);
            Assert.NotNull(sharedView.PreparationEmptyState);
            Assert.NotNull(sharedView.PreparationMaterialSelectedState);
            Assert.NotNull(sharedView.PreparationDragable);
            Assert.NotNull(sharedView.PreparationInteractor);
            Assert.NotNull(sharedView.PreparationEmptyAttemptListener);
            var sharedSize = ((RectTransform)sharedPrefab.transform).sizeDelta;
            var sharedAspect = sharedSize.x / sharedSize.y;

            var slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationSlotItem.prefab");
            Assert.NotNull(slotPrefab);
            var slotView = slotPrefab.GetComponent<PreparationSlotItemView>();
            Assert.NotNull(slotView);
            Assert.IsTrue(slotView.ArtworkArea.preserveAspect);
            AssertFrameCoversEntireCard(slotView.CardFrame.rectTransform);
            var slotSize = ((RectTransform)slotPrefab.transform).sizeDelta;
            Assert.AreEqual(sharedAspect, slotSize.x / slotSize.y, 0.0001f);

            var fusionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationFusionSlotItem.prefab");
            Assert.NotNull(fusionPrefab);
            var fusionView = fusionPrefab.GetComponent<PreparationFusionSlotItemView>();
            Assert.NotNull(fusionView);
            Assert.IsTrue(fusionView.ArtworkArea.preserveAspect);
            AssertFrameCoversEntireCard(fusionView.CardFrame.rectTransform);
            var fusionSize = ((RectTransform)fusionPrefab.transform).sizeDelta;
            Assert.AreEqual(sharedAspect, fusionSize.x / fusionSize.y, 0.0001f);

            var preparationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resources/Ui/PreparationView.prefab");
            Assert.NotNull(preparationPrefab);
            var preparationView = preparationPrefab.GetComponent<PreparationView>();
            Assert.NotNull(preparationView);
            var poolSlotSize = preparationView.CardPoolList.ConstantSlotSize;
            Assert.AreEqual(sharedAspect, poolSlotSize.x / poolSlotSize.y, 0.0001f);
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
            };
            var card = new BattleCardRawComponent();
            var random = new Random(12345u);

            card.Initialize(EBattleSide.Enemy, 2, cardConfig, typeConfig, ref random);

            Assert.AreEqual(7, card.CardNumber);
            Assert.AreEqual(3, card.CardTypeId);
            Assert.AreEqual(EBattleSide.Enemy, card.Side);
            Assert.AreEqual(2, card.SlotIndex);
            Assert.That(card.Attack, Is.InRange(4, 6));
            Assert.That(card.MaxHealth, Is.InRange(8, 10));
            Assert.AreEqual(card.MaxHealth, card.CurrentHealth.Value);
            Assert.IsTrue(card.IsAlive.Value);

            card.CollectToPool();

            Assert.AreEqual(0, card.CardNumber);
            Assert.AreEqual(0, card.CardTypeId);
            Assert.AreEqual(0, card.Attack);
            Assert.AreEqual(0, card.MaxHealth);
            Assert.AreEqual(0, card.CurrentHealth.Value);
            Assert.IsFalse(card.IsAlive.Value);
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
            var fusionCard = DataApi.GetData<BattleCardCsvData>(RunCardRules.FusionResultCardNumber);
            Assert.NotNull(fusionCard);
            Assert.AreEqual(6, fusionCard.CardTypeId);
            Assert.AreEqual("FusionCard_099", fusionCard.ArtworkKey);
            Assert.NotNull(ResourceApi.LoadSprite("FusionCard_099"));

            var fusionType = DataApi.GetData<BattleCardTypeCsvData>(6);
            Assert.NotNull(fusionType);
            Assert.AreEqual("融合造物", fusionType.DisplayName);
            Assert.AreEqual(1, fusionType.MinHealth);
            Assert.AreEqual(1, fusionType.MaxHealth);
            Assert.AreEqual(0, fusionType.MinAttack);
            Assert.AreEqual(0, fusionType.MaxAttack);

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

        private static void AssertFrameCoversEntireCard(RectTransform frame)
        {
            Assert.AreEqual(Vector2.zero, frame.anchorMin);
            Assert.AreEqual(Vector2.one, frame.anchorMax);
            Assert.AreEqual(Vector2.zero, frame.anchoredPosition);
            Assert.AreEqual(Vector2.zero, frame.sizeDelta);
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
    }
}
