using System;
using System.Collections.Generic;
using BbxCommon;
using Random = Unity.Mathematics.Random;

namespace Hearthstone
{
    public sealed class BattlePlayerLineupStartupData
    {
        private readonly RunCardInstanceData[] m_Slots;

        public int SlotCount => m_Slots.Length;

        public BattlePlayerLineupStartupData(IReadOnlyList<RunCardInstanceData> slots)
        {
            if (slots == null || slots.Count < RunCardRules.InitialBattleSlotCount ||
                slots.Count > RunCardRules.MaximumBattleSlotCount)
            {
                throw new ArgumentException(
                    $"A player lineup must contain between {RunCardRules.InitialBattleSlotCount} and " +
                    $"{RunCardRules.MaximumBattleSlotCount} slots.",
                    nameof(slots));
            }
            m_Slots = new RunCardInstanceData[slots.Count];
            var occupiedCards = new HashSet<int>();
            for (var slot = 0; slot < m_Slots.Length; slot++)
            {
                var card = slots[slot];
                if (card.IsValid && occupiedCards.Add(card.CardNumber) == false)
                    throw new ArgumentException($"Player lineup repeats card {card.CardNumber}.", nameof(slots));
                m_Slots[slot] = card;
            }
        }

        public RunCardInstanceData GetSlot(int slot)
        {
            if (slot < 0 || slot >= m_Slots.Length)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return m_Slots[slot];
        }

        public BattlePlayerLineupStartupData CreateSnapshot() => new BattlePlayerLineupStartupData(m_Slots);

        public static BattlePlayerLineupStartupData Capture(
            RunStateSingletonRawComponent runState,
            IReadOnlyList<int> battleSlotCardNumbers,
            int slotCount)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));
            if (battleSlotCardNumbers == null || slotCount < RunCardRules.InitialBattleSlotCount ||
                slotCount > battleSlotCardNumbers.Count ||
                slotCount > RunCardRules.MaximumBattleSlotCount)
                throw new ArgumentException("Battle slot snapshot has an invalid length.", nameof(battleSlotCardNumbers));
            var slots = new RunCardInstanceData[slotCount];
            for (var slot = 0; slot < slots.Length; slot++)
            {
                var cardNumber = battleSlotCardNumbers[slot];
                if (cardNumber == 0)
                    continue;
                if (runState.HasCard(cardNumber) == false)
                    throw new InvalidOperationException($"Battle slot {slot} references unowned card {cardNumber}.");
                slots[slot] = runState.CardInstances[cardNumber];
            }
            return new BattlePlayerLineupStartupData(slots);
        }

        public static BattlePlayerLineupStartupData Capture(
            RunStateSingletonRawComponent runState,
            IReadOnlyList<int> battleSlotCardNumbers)
        {
            return Capture(runState, battleSlotCardNumbers, RunCardRules.InitialBattleSlotCount);
        }
    }

    public sealed class RewardCardGrantStartupData
    {
        public int CardNumber { get; }
        public int Attack { get; }
        public int MaxHealth { get; }

        public RewardCardGrantStartupData(int cardNumber, int attack, int maxHealth)
        {
            if (cardNumber < RunCardRules.FirstCardNumber || cardNumber > RunCardRules.LastOrdinaryCardNumber)
                throw new ArgumentOutOfRangeException(nameof(cardNumber));
            if (attack < 0)
                throw new ArgumentOutOfRangeException(nameof(attack));
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));

            CardNumber = cardNumber;
            Attack = attack;
            MaxHealth = maxHealth;
        }

        public RewardCardGrantStartupData CreateSnapshot()
        {
            return new RewardCardGrantStartupData(CardNumber, Attack, MaxHealth);
        }
    }

    public sealed class PreparationRewardBatchStartupData
    {
        private readonly RewardCardGrantStartupData[] m_Grants;

        public string BatchId { get; }
        public IReadOnlyList<RewardCardGrantStartupData> Grants => m_Grants;

        public PreparationRewardBatchStartupData(
            string batchId,
            IReadOnlyList<RewardCardGrantStartupData> grants)
        {
            if (string.IsNullOrWhiteSpace(batchId))
                throw new ArgumentException("Reward batch id cannot be empty.", nameof(batchId));
            if (grants == null)
                throw new ArgumentNullException(nameof(grants));

            BatchId = batchId;
            m_Grants = new RewardCardGrantStartupData[grants.Count];
            for (var index = 0; index < m_Grants.Length; index++)
            {
                var grant = grants[index] ?? throw new ArgumentException("Reward grant cannot be null.", nameof(grants));
                m_Grants[index] = grant.CreateSnapshot();
            }
        }

        public PreparationRewardBatchStartupData CreateSnapshot()
        {
            return new PreparationRewardBatchStartupData(BatchId, m_Grants);
        }
    }

    public static class PreparationRewardBatchFactory
    {
        public static PreparationRewardBatchStartupData CreateRandom(
            string batchId,
            Predicate<int> isUnavailable,
            int drawCount,
            ref Random random)
        {
            if (string.IsNullOrWhiteSpace(batchId))
                throw new ArgumentException("Reward batch id cannot be empty.", nameof(batchId));
            if (random.state == 0)
                throw new ArgumentException("Reward random state cannot be zero.", nameof(random));
            if (drawCount < 0)
                throw new ArgumentOutOfRangeException(nameof(drawCount));

            var candidates = new int[
                RunCardRules.LastOrdinaryCardNumber - RunCardRules.FirstCardNumber + 1];
            var candidateCount = 0;
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastOrdinaryCardNumber;
                 cardNumber++)
            {
                var card = DataApi.GetData<BattleCardCsvData>(cardNumber)
                    ?? throw new InvalidOperationException($"Reward card configuration {cardNumber} is missing.");
                if (DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId) == null)
                    throw new InvalidOperationException($"Reward card type {card.CardTypeId} is missing.");
                if (isUnavailable != null && isUnavailable(cardNumber))
                    continue;
                candidates[candidateCount++] = cardNumber;
            }

            if (candidateCount < drawCount)
            {
                throw new InvalidOperationException(
                    $"Only {candidateCount} ordinary reward cards are available; " +
                    $"{drawCount} are required.");
            }

            var grants = new RewardCardGrantStartupData[drawCount];
            for (var index = 0; index < grants.Length; index++)
            {
                var selectedIndex = random.NextInt(index, candidateCount);
                var cardNumber = candidates[selectedIndex];
                candidates[selectedIndex] = candidates[index];
                candidates[index] = cardNumber;

                var card = DataApi.GetData<BattleCardCsvData>(cardNumber);
                var type = DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId);
                grants[index] = new RewardCardGrantStartupData(
                    cardNumber,
                    type.RollAttack(ref random),
                    type.RollHealth(ref random));
            }
            return new PreparationRewardBatchStartupData(batchId, grants);
        }

        public static PreparationRewardBatchStartupData CreateRandom(
            string batchId,
            Predicate<int> isUnavailable,
            ref Random random)
        {
            return CreateRandom(batchId, isUnavailable, RunCardRules.RewardGrantCount, ref random);
        }
    }

    public static class EnemyCardFactory
    {
        public static RunCardInstanceData Create(int cardNumber, ref Random random)
        {
            return BattleCardSimulationFactory.Create(cardNumber, ref random);
        }
    }

    public sealed class EnemyBattlePreviewStartupData
    {
        private readonly RunCardInstanceData[] m_Cards;

        public int BattleNumber { get; }
        public uint RandomSeed { get; }
        public uint TargetRandomState { get; }
        public int CardCount => m_Cards.Length;

        public EnemyBattlePreviewStartupData(
            int battleNumber,
            uint randomSeed,
            uint targetRandomState,
            IReadOnlyList<RunCardInstanceData> cards)
        {
            if (battleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            if (randomSeed == 0)
                throw new ArgumentOutOfRangeException(nameof(randomSeed));
            if (targetRandomState == 0)
                throw new ArgumentOutOfRangeException(nameof(targetRandomState));
            if (cards == null || cards.Count <= 0 ||
                cards.Count > RunCardRules.MaximumBattleSlotCount)
                throw new ArgumentException("Enemy preview card count is invalid.", nameof(cards));

            BattleNumber = battleNumber;
            RandomSeed = randomSeed;
            TargetRandomState = targetRandomState;
            m_Cards = new RunCardInstanceData[cards.Count];
            for (var index = 0; index < m_Cards.Length; index++)
            {
                var card = cards[index];
                if (card.IsValid == false)
                    throw new ArgumentException("Enemy preview contains an invalid card instance.", nameof(cards));
                m_Cards[index] = card;
            }
        }

        public RunCardInstanceData GetCard(int slot)
        {
            if (slot < 0 || slot >= m_Cards.Length)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return m_Cards[slot];
        }

        public EnemyBattlePreviewStartupData CreateSnapshot()
        {
            return new EnemyBattlePreviewStartupData(
                BattleNumber,
                RandomSeed,
                TargetRandomState,
                m_Cards);
        }

        public static EnemyBattlePreviewStartupData CreateRandom(int battleNumber, uint randomSeed)
        {
            var normalizedSeed = BattleRules.NormalizeSeed(randomSeed);
            var random = new Random(normalizedSeed);
            var lineup = EnemyLineupCsvData.GetRandomRequired(battleNumber, ref random);
            var cards = new RunCardInstanceData[lineup.CardNumbers.Length];
            for (var slot = 0; slot < cards.Length; slot++)
                cards[slot] = EnemyCardFactory.Create(lineup.CardNumbers[slot], ref random);
            return new EnemyBattlePreviewStartupData(
                battleNumber,
                normalizedSeed,
                random.state,
                cards);
        }
    }

    public sealed class PreparationRoundStartupData
    {
        public int BattleNumber { get; }
        public int UnlockedBattleSlotCount { get; }
        public PreparationRewardBatchStartupData RewardBatch { get; }
        public EnemyBattlePreviewStartupData EnemyPreview { get; }

        public PreparationRoundStartupData(
            int battleNumber,
            int unlockedBattleSlotCount,
            PreparationRewardBatchStartupData rewardBatch,
            EnemyBattlePreviewStartupData enemyPreview = null)
        {
            if (battleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            if (unlockedBattleSlotCount < RunCardRules.InitialBattleSlotCount ||
                unlockedBattleSlotCount > RunCardRules.MaximumBattleSlotCount)
                throw new ArgumentOutOfRangeException(nameof(unlockedBattleSlotCount));
            BattleNumber = battleNumber;
            UnlockedBattleSlotCount = unlockedBattleSlotCount;
            RewardBatch = rewardBatch?.CreateSnapshot() ?? throw new ArgumentNullException(nameof(rewardBatch));
            if (enemyPreview != null && enemyPreview.BattleNumber != battleNumber)
                throw new ArgumentException("Enemy preview battle number does not match the round.", nameof(enemyPreview));
            EnemyPreview = enemyPreview?.CreateSnapshot();
        }

        public PreparationRoundStartupData CreateSnapshot()
        {
            return new PreparationRoundStartupData(
                BattleNumber,
                UnlockedBattleSlotCount,
                RewardBatch,
                EnemyPreview);
        }
    }

    public sealed class BattleStageStartupData
    {
        public int BattleNumber { get; }
        public PreparationRewardBatchStartupData PreparationRewardBatch { get; }
        public BattleScenarioStartupData Scenario { get; }
        public BattlePlayerLineupStartupData ContinuePlayerLineup { get; }
        public EnemyBattlePreviewStartupData EnemyPreview { get; }

        public BattleStageStartupData(PreparationRewardBatchStartupData preparationRewardBatch)
            : this(1, preparationRewardBatch, null)
        {
        }

        public BattleStageStartupData(
            PreparationRewardBatchStartupData preparationRewardBatch,
            BattleScenarioStartupData scenario)
            : this(1, preparationRewardBatch, scenario)
        {
        }

        public BattleStageStartupData(
            int battleNumber,
            PreparationRewardBatchStartupData preparationRewardBatch,
            BattleScenarioStartupData scenario = null,
            BattlePlayerLineupStartupData continuePlayerLineup = null,
            EnemyBattlePreviewStartupData enemyPreview = null)
        {
            if (battleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            BattleNumber = battleNumber;
            PreparationRewardBatch = preparationRewardBatch?.CreateSnapshot()
                ?? throw new ArgumentNullException(nameof(preparationRewardBatch));
            if (scenario != null && continuePlayerLineup != null)
                throw new ArgumentException("A battle cannot use both an explicit scenario and a Continue lineup snapshot.");
            if (scenario != null && enemyPreview != null)
                throw new ArgumentException("An explicit battle scenario cannot use an enemy preview snapshot.");
            if (enemyPreview != null && enemyPreview.BattleNumber != battleNumber)
                throw new ArgumentException("Enemy preview battle number does not match the battle.", nameof(enemyPreview));
            Scenario = scenario?.CreateSnapshot();
            ContinuePlayerLineup = continuePlayerLineup?.CreateSnapshot();
            EnemyPreview = enemyPreview?.CreateSnapshot();
        }

        public BattleStageStartupData CreateSnapshot()
        {
            return new BattleStageStartupData(
                BattleNumber,
                PreparationRewardBatch,
                Scenario,
                ContinuePlayerLineup,
                EnemyPreview);
        }

        public static BattleStageStartupData CreateDefault()
        {
            return CreateDefault(BattleRules.NormalizeSeed(unchecked((uint)DateTime.UtcNow.Ticks)));
        }

        public static BattleStageStartupData CreateDefault(uint rewardRandomSeed)
        {
            var random = new Random(BattleRules.NormalizeSeed(rewardRandomSeed));
            var rewardBatch = PreparationRewardBatchFactory.CreateRandom(
                "battle-001-reward",
                IsInitialPlayerCard,
                ref random);
            return new BattleStageStartupData(1, rewardBatch);
        }

        private static bool IsInitialPlayerCard(int cardNumber)
        {
            for (var slot = 0; slot < BattleRules.CardsPerSide; slot++)
            {
                if (BattleRules.GetCardNumber(EBattleSide.Player, slot) == cardNumber)
                    return true;
            }
            return false;
        }
    }
}
