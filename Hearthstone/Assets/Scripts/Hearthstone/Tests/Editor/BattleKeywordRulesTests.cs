using System;
using BbxCommon;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hearthstone.Tests
{
    public sealed class BattleKeywordRulesTests
    {
        private const string DefaultKeywordCsv =
            "Keyword,DisplayName,Description,DisplayOrder,DamageNumerator,DamageDenominator,BlastDistance,AttackGain,HealthGain,SuppressCounterDamage\n" +
            "// Unique single keyword flag,Chinese display name,Player-facing rule description,Stable display order,Damage scale numerator,Damage scale denominator,Adjacent slot distance,Attack gain per trigger,Health gain per trigger,Whether counter damage is suppressed\n" +
            "// Associated: BattleCardTypeCsvData\n" +
            "Taunt,嘲讽,敌方存在嘲讽单位时只能以嘲讽单位为攻击目标。,0,1,1,0,0,0,false\n" +
            "LongShot,远射,攻击伤害减半并且不会受到目标反击。,1,1,2,0,0,0,true\n" +
            "Blast,爆裂,攻击时对目标相邻1格内的存活单位造成主目标伤害的一半。,2,1,2,1,0,0,false\n" +
            "Charge,冲锋,每次发动攻击前使己方所有存活单位的攻击和生命各提高1点。,3,1,1,0,1,1,false\n";

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
            CsvApi.ReadFromString<BattleKeywordCsvData>(nameof(BattleKeywordCsvData), DefaultKeywordCsv);
        }

        [TearDown]
        public void TearDown()
        {
            DataApi.ReleaseAllData<BattleKeywordCsvData>(false);
            DataApi.ReleaseAllData<BattleCardCsvData>(false);
            DataApi.ReleaseAllData<BattleCardTypeCsvData>(false);
        }

        [Test]
        public void KeywordUnionIsNormalizedIdempotentAndUsesStableDisplayOrder()
        {
            var result = BattleKeywordRules.UnionKeywords(
                EBattleKeyword.Blast | EBattleKeyword.Charge,
                EBattleKeyword.Blast | EBattleKeyword.Taunt,
                EBattleKeyword.None | EBattleKeyword.LongShot);

            Assert.AreEqual(BattleKeywordRules.AllKeywords, result);
            Assert.AreEqual(result, BattleKeywordRules.UnionKeywords(result, result));
            Assert.AreEqual("嘲讽、远射、爆裂、冲锋", BattleKeywordRules.FormatDisplayText(result));
            Assert.AreEqual("嘲讽", BattleKeywordRules.FormatDisplayText(EBattleKeyword.Taunt));
            Assert.AreEqual(string.Empty, BattleKeywordRules.FormatDisplayText(EBattleKeyword.None));
            Assert.AreEqual(
                "远射：攻击伤害减半并且不会受到目标反击。\n" +
                "冲锋：每次发动攻击前使己方所有存活单位的攻击和生命各提高1点。",
                BattleKeywordRules.FormatDescriptionText(
                    EBattleKeyword.LongShot | EBattleKeyword.Charge));
            Assert.AreEqual(string.Empty, BattleKeywordRules.FormatDescriptionText(EBattleKeyword.None));
        }

        [Test]
        public void TypeAndKeywordCsvParseAllConfiguredMappings()
        {
            const string typeCsv =
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack,InitialKeyword\n" +
                "// Unique card type identifier,Display name shown on cards,Minimum generated health inclusive,Maximum generated health inclusive,Minimum generated attack inclusive,Maximum generated attack inclusive,Initial battle keyword or None\n" +
                "// Associated: BattleCardCsvData\n" +
                "1,哥布林战士,5,7,2,4,Taunt\n" +
                "2,哥布林弓手,3,5,3,5,LongShot\n" +
                "3,哥布林投弹手,2,4,4,6,Blast\n" +
                "4,野猪,4,6,3,5,Charge\n" +
                "5,食人魔,6,8,5,7,None\n";

            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), typeCsv);

            Assert.AreEqual(EBattleKeyword.Taunt, DataApi.GetData<BattleCardTypeCsvData>(1).InitialKeyword);
            Assert.AreEqual(EBattleKeyword.LongShot, DataApi.GetData<BattleCardTypeCsvData>(2).InitialKeyword);
            Assert.AreEqual(EBattleKeyword.Blast, DataApi.GetData<BattleCardTypeCsvData>(3).InitialKeyword);
            Assert.AreEqual(EBattleKeyword.Charge, DataApi.GetData<BattleCardTypeCsvData>(4).InitialKeyword);
            Assert.AreEqual(EBattleKeyword.None, DataApi.GetData<BattleCardTypeCsvData>(5).InitialKeyword);
            Assert.AreEqual("爆裂", DataApi.GetData<BattleKeywordCsvData>((int)EBattleKeyword.Blast).DisplayName);
            Assert.AreEqual(2, DataApi.GetData<BattleKeywordCsvData>((int)EBattleKeyword.LongShot).DamageDenominator);
            Assert.AreEqual(1, DataApi.GetData<BattleKeywordCsvData>((int)EBattleKeyword.Blast).BlastDistance);
            Assert.AreEqual(1, DataApi.GetData<BattleKeywordCsvData>((int)EBattleKeyword.Charge).AttackGain);
        }

        [Test]
        public void TypeCsvAllowsMissingLegacyColumnButRejectsInvalidOrCombinedKeyword()
        {
            const string legacyCsv =
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack\n" +
                "// id,name,min hp,max hp,min attack,max attack\n" +
                "// Associated: BattleCardCsvData\n" +
                "7,旧类型,1,1,0,0\n";
            CsvApi.ReadFromString<BattleCardTypeCsvData>(nameof(BattleCardTypeCsvData), legacyCsv);
            Assert.AreEqual(EBattleKeyword.None, DataApi.GetData<BattleCardTypeCsvData>(7).InitialKeyword);

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "Battle card type 8 must configure exactly one known initial keyword"));
            CsvApi.ReadFromString<BattleCardTypeCsvData>(
                nameof(BattleCardTypeCsvData),
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack,InitialKeyword\n" +
                "// id,name,min hp,max hp,min attack,max attack,keyword\n" +
                "// Associated: BattleCardCsvData\n8,组合,1,1,0,0,3\n");
            Assert.IsNull(DataApi.GetData<BattleCardTypeCsvData>(8));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "Battle card type 9 has invalid initial keyword"));
            CsvApi.ReadFromString<BattleCardTypeCsvData>(
                nameof(BattleCardTypeCsvData),
                "CardTypeId,DisplayName,MinHealth,MaxHealth,MinAttack,MaxAttack,InitialKeyword\n" +
                "// id,name,min hp,max hp,min attack,max attack,keyword\n" +
                "// Associated: BattleCardCsvData\n9,非法,1,1,0,0,UnknownKeyword\n");
            Assert.IsNull(DataApi.GetData<BattleCardTypeCsvData>(9));
        }

        [Test]
        public void TargetAndDamageRulesCoverTauntLongShotBlastAndEmptyAdjacency()
        {
            const uint alive = (1u << 0) | (1u << 1) | (1u << 2);
            Assert.AreEqual(1u << 1, BattleRules.FilterTargetCandidateMask(alive, 1u << 1));
            Assert.AreEqual(alive, BattleRules.FilterTargetCandidateMask(alive, 0u));
            var blastDistance = BattleKeywordRules.GetConfig(EBattleKeyword.Blast).BlastDistance;
            Assert.AreEqual((1u << 0) | (1u << 2), BattleRules.GetAdjacentLivingMask(1, alive, blastDistance));
            Assert.AreEqual(0u, BattleRules.GetAdjacentLivingMask(0, 1u << 2, blastDistance));

            var damage = BattleRules.ResolveKeywordDamage(
                7,
                9,
                EBattleKeyword.LongShot | EBattleKeyword.Blast);
            Assert.AreEqual(3, damage.MainDamage);
            Assert.AreEqual(1, damage.BlastDamage);
            Assert.AreEqual(0, damage.CounterDamage);

            damage = BattleRules.ResolveKeywordDamage(1, 9, EBattleKeyword.LongShot);
            Assert.AreEqual(0, damage.MainDamage);
            Assert.AreEqual(0, damage.BlastDamage);
            Assert.AreEqual(0, damage.CounterDamage);
        }

        [Test]
        public void DamageAndChargeBehaviorUseKeywordConfigurationValues()
        {
            DataApi.ReleaseAllData<BattleKeywordCsvData>(false);
            const string customCsv =
                "Keyword,DisplayName,Description,DisplayOrder,DamageNumerator,DamageDenominator,BlastDistance,AttackGain,HealthGain,SuppressCounterDamage\n" +
                "// keyword,name,description,order,numerator,denominator,distance,attack gain,health gain,suppress counter\n" +
                "// Associated: BattleCardTypeCsvData\n" +
                "Taunt,嘲讽,嘲讽说明,0,1,1,0,0,0,false\n" +
                "LongShot,远射,远射说明,1,2,3,0,0,0,false\n" +
                "Blast,爆裂,爆裂说明,2,1,3,2,0,0,false\n" +
                "Charge,冲锋,冲锋说明,3,1,1,0,2,3,false\n";
            CsvApi.ReadFromString<BattleKeywordCsvData>(nameof(BattleKeywordCsvData), customCsv);

            var damage = BattleRules.ResolveKeywordDamage(9, 5, EBattleKeyword.LongShot | EBattleKeyword.Blast);
            Assert.AreEqual(6, damage.MainDamage);
            Assert.AreEqual(2, damage.BlastDamage);
            Assert.AreEqual(5, damage.CounterDamage);
            Assert.AreEqual((1u << 1) | (1u << 2), BattleRules.GetAdjacentLivingMask(0, BattleRules.InitialAliveMask, 2));
            Assert.AreEqual(2, BattleKeywordRules.GetConfig(EBattleKeyword.Charge).AttackGain);
            Assert.AreEqual(3, BattleKeywordRules.GetConfig(EBattleKeyword.Charge).HealthGain);
        }

        [Test]
        public void FusionUnionsAllMaterialKeywordsAndRejectsLockedAndFusionCardsAsMaterials()
        {
            var runState = new RunStateSingletonRawComponent();
            var session = new PreparationSessionSingletonRawComponent();
            var cards = new[]
            {
                new RunCardInstanceData(14, 2, 20, EBattleKeyword.LongShot),
                new RunCardInstanceData(20, 2, 20, EBattleKeyword.Blast),
                new RunCardInstanceData(30, 2, 20, EBattleKeyword.Blast),
                new RunCardInstanceData(35, 1, 20, EBattleKeyword.None),
            };
            for (var index = 0; index < cards.Length; index++)
            {
                runState.CardInstances[cards[index].CardNumber] = cards[index];
                Assert.AreEqual(
                    EFusionOperationResult.Applied,
                    RunCardRules.TrySetFusionMaterial(runState, session, cards[index].CardNumber, index));
            }

            Assert.AreEqual(
                EFusionOperationResult.Applied,
                RunCardRules.TryFuse(runState, session, out var result, out var transaction));
            Assert.AreEqual(7, result.Attack);
            Assert.AreEqual(80, result.MaxHealth);
            Assert.AreEqual(EBattleKeyword.LongShot | EBattleKeyword.Blast, result.Keywords);
            Assert.AreEqual(result.Keywords, transaction.ResultCard.Keywords);
            Assert.AreEqual(
                EFusionOperationResult.ResultCardCannotBeMaterial,
                RunCardRules.TrySetFusionMaterial(runState, session, RunCardRules.LockedCardNumber, 0));
            Assert.AreEqual(
                EFusionOperationResult.ResultCardCannotBeMaterial,
                RunCardRules.TrySetFusionMaterial(runState, session, result.CardNumber, 0));
        }

        [Test]
        public void ScenarioDataSupportsEmptyAndExplicitSlotsWithDefensiveCopies()
        {
            var players = new[]
            {
                BattleCardSlotStartupData.FromRunState(14),
                BattleCardSlotStartupData.Explicit(20, 7, 80, 33),
                BattleCardSlotStartupData.Empty,
                BattleCardSlotStartupData.Empty,
                BattleCardSlotStartupData.Empty,
                BattleCardSlotStartupData.Empty,
            };
            var enemies = new[]
            {
                BattleCardSlotStartupData.Explicit(40, 2, 20, 20),
                BattleCardSlotStartupData.Empty,
                BattleCardSlotStartupData.Explicit(44, 2, 20, 0),
                BattleCardSlotStartupData.Empty,
                BattleCardSlotStartupData.Empty,
                BattleCardSlotStartupData.Empty,
            };
            var scenario = new BattleScenarioStartupData(players, enemies, 123u);
            players[0] = BattleCardSlotStartupData.Empty;
            enemies[0] = BattleCardSlotStartupData.Empty;

            Assert.IsTrue(scenario.GetPlayerSlot(0).IsOccupied);
            Assert.AreEqual(14, scenario.GetPlayerSlot(0).CardNumber);
            Assert.IsFalse(scenario.GetEnemySlot(1).IsOccupied);
            Assert.AreEqual(0, scenario.GetEnemySlot(2).CurrentHealth);
            Assert.AreEqual(scenario.GetPlayerSlot(0), scenario.CreateSnapshot().GetPlayerSlot(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BattleScenarioStartupData(players, enemies, 0u));
            Assert.Throws<ArgumentException>(() =>
                new BattleScenarioStartupData(
                    new[] { BattleCardSlotStartupData.Empty, BattleCardSlotStartupData.Empty, BattleCardSlotStartupData.Empty },
                    new[] { BattleCardSlotStartupData.FromRunState(40), BattleCardSlotStartupData.Empty, BattleCardSlotStartupData.Empty },
                    1u));
        }

        [Test]
        public void BattleCardKeepsLegacyAttackAndSynchronizesObservableMirrorAndChargeGain()
        {
            var cardConfig = new BattleCardCsvData { CardNumber = 7, CardTypeId = 4 };
            var typeConfig = new BattleCardTypeCsvData
            {
                CardTypeId = 4,
                InitialKeyword = EBattleKeyword.Charge,
            };
            var card = new BattleCardRawComponent();
            card.InitializeExplicit(EBattleSide.Player, 0, cardConfig, typeConfig, 3, 5, 5);
            Assert.AreEqual(3, card.Attack);
            Assert.AreEqual(3, card.AttackValue.Value);
            Assert.AreEqual(EBattleKeyword.Charge, card.Keywords);

            card.Attack = 8;
            card.SyncAttackValue();
            Assert.AreEqual(8, card.AttackValue.Value);
            card.ApplyBattleStatGain(1, 1);
            Assert.AreEqual(9, card.Attack);
            Assert.AreEqual(9, card.AttackValue.Value);
            Assert.AreEqual(6, card.MaxHealth);
            Assert.AreEqual(6, card.CurrentHealth.Value);

            card.SetCurrentHealthWithoutAliveCommit(0);
            Assert.AreEqual(0, card.CurrentHealth.Value);
            Assert.IsTrue(card.IsAlive.Value, "Death must remain deferred until the whole damage batch commits.");
            card.CommitAliveState();
            Assert.IsFalse(card.IsAlive.Value);
            card.ApplyBattleStatGain(1, 1);
            Assert.AreEqual(9, card.Attack, "Charge must ignore cards that are already dead.");

            card.CollectToPool();
            Assert.AreEqual(0, card.Attack);
            Assert.AreEqual(0, card.AttackValue.Value);
            Assert.AreEqual(EBattleKeyword.None, card.Keywords);
        }

        [Test]
        public void SharedBattleCardExposesKeywordTextForBattleAndPreparationLists()
        {
            AssertKeywordText<BattleCardItemView>("Assets/Resources/Ui/BattleCardItem.prefab", view => view.KeywordText);
        }

        private static void AssertKeywordText<TView>(string path, Func<TView, TMPro.TMP_Text> selector)
            where TView : Component
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.NotNull(prefab, path);
            var view = prefab.GetComponent<TView>();
            Assert.NotNull(view, path);
            var text = selector(view);
            Assert.NotNull(text, path);
            Assert.IsTrue(text.enableAutoSizing, path);
            Assert.IsTrue(text.enableWordWrapping, path);
            Assert.LessOrEqual(text.fontSizeMin, 10f, path);
            Assert.GreaterOrEqual(text.fontSizeMax, 17f, path);
            Assert.AreEqual(TMPro.TextAlignmentOptions.Top, text.alignment, path);
        }
    }
}
