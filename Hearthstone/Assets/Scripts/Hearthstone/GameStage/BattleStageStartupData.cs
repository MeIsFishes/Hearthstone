using System;
using System.Collections.Generic;

namespace Hearthstone
{
    public sealed class RewardCardGrantStartupData
    {
        public int CardNumber { get; }
        public int Attack { get; }
        public int MaxHealth { get; }

        public RewardCardGrantStartupData(int cardNumber, int attack, int maxHealth)
        {
            if (cardNumber < RunCardRules.FirstCardNumber || cardNumber > RunCardRules.LastCardNumber)
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
        public PreparationRewardBatchStartupData PreparationRewardBatch { get; }

        public BattleStageStartupData(PreparationRewardBatchStartupData preparationRewardBatch)
        {
            PreparationRewardBatch = preparationRewardBatch?.CreateSnapshot()
                ?? throw new ArgumentNullException(nameof(preparationRewardBatch));
        }

        public BattleStageStartupData CreateSnapshot()
        {
            return new BattleStageStartupData(PreparationRewardBatch);
        }

        public static BattleStageStartupData CreateDefault()
        {
            return new BattleStageStartupData(new PreparationRewardBatchStartupData(
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
