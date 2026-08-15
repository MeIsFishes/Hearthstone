using System;
using System.Collections.Generic;

namespace Hearthstone
{
    public sealed class BattlePlayerLineupStartupData
    {
        private readonly RunCardInstanceData[] m_Slots;

        public int SlotCount => m_Slots.Length;

        public BattlePlayerLineupStartupData(IReadOnlyList<RunCardInstanceData> slots)
        {
            if (slots == null || slots.Count != RunCardRules.BattleSlotCount)
                throw new ArgumentException($"A player lineup must contain exactly {RunCardRules.BattleSlotCount} slots.", nameof(slots));
            m_Slots = new RunCardInstanceData[RunCardRules.BattleSlotCount];
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
            IReadOnlyList<int> battleSlotCardNumbers)
        {
            if (runState == null)
                throw new ArgumentNullException(nameof(runState));
            if (battleSlotCardNumbers == null || battleSlotCardNumbers.Count != RunCardRules.BattleSlotCount)
                throw new ArgumentException("Battle slot snapshot has an invalid length.", nameof(battleSlotCardNumbers));
            var slots = new RunCardInstanceData[RunCardRules.BattleSlotCount];
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
            if (grants == null || grants.Count != RunCardRules.RewardGrantCount)
            {
                throw new ArgumentException(
                    $"A reward batch must contain exactly {RunCardRules.RewardGrantCount} grants.",
                    nameof(grants));
            }

            BatchId = batchId;
            m_Grants = new RewardCardGrantStartupData[RunCardRules.RewardGrantCount];
            var visited = new HashSet<int>();
            for (var index = 0; index < m_Grants.Length; index++)
            {
                var grant = grants[index] ?? throw new ArgumentException("Reward grant cannot be null.", nameof(grants));
                if (visited.Add(grant.CardNumber) == false)
                    throw new ArgumentException($"Reward card {grant.CardNumber} is duplicated.", nameof(grants));
                m_Grants[index] = grant.CreateSnapshot();
            }
        }

        public PreparationRewardBatchStartupData CreateSnapshot()
        {
            return new PreparationRewardBatchStartupData(BatchId, m_Grants);
        }
    }

    public sealed class BattleStageStartupData
    {
        public int BattleNumber { get; }
        public PreparationRewardBatchStartupData PreparationRewardBatch { get; }
        public BattleScenarioStartupData Scenario { get; }
        public BattlePlayerLineupStartupData ContinuePlayerLineup { get; }

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
            BattlePlayerLineupStartupData continuePlayerLineup = null)
        {
            if (battleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            BattleNumber = battleNumber;
            PreparationRewardBatch = preparationRewardBatch?.CreateSnapshot()
                ?? throw new ArgumentNullException(nameof(preparationRewardBatch));
            if (scenario != null && continuePlayerLineup != null)
                throw new ArgumentException("A battle cannot use both an explicit scenario and a Continue lineup snapshot.");
            Scenario = scenario?.CreateSnapshot();
            ContinuePlayerLineup = continuePlayerLineup?.CreateSnapshot();
        }

        public BattleStageStartupData CreateSnapshot()
        {
            return new BattleStageStartupData(BattleNumber, PreparationRewardBatch, Scenario, ContinuePlayerLineup);
        }

        public static BattleStageStartupData CreateDefault()
        {
            return new BattleStageStartupData(1, new PreparationRewardBatchStartupData(
                "initial-battle-reward-001",
                new[]
                {
                    new RewardCardGrantStartupData(2, 5, 3),
                    new RewardCardGrantStartupData(3, 4, 4),
                    new RewardCardGrantStartupData(5, 3, 5),
                    new RewardCardGrantStartupData(6, 5, 4),
                    new RewardCardGrantStartupData(7, 6, 2),
                }));
        }
    }
}
