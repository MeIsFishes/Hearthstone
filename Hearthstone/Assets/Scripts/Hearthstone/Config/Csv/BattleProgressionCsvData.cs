using System;
using System.Collections.Generic;
using BbxCommon;

namespace Hearthstone
{
    /// <summary>
    /// Formal, deterministic settlement reward definition for one battle number.
    /// </summary>
    public sealed class BattleProgressionCsvData : CsvDataBase<BattleProgressionCsvData>
    {
        public int BattleNumber { get; private set; }
        public string SettlementRewardBatchId { get; private set; }
        public IReadOnlyList<int> RewardCardNumbers => m_RewardCardNumbers;
        public IReadOnlyList<int> RewardAttacks => m_RewardAttacks;
        public IReadOnlyList<int> RewardMaxHealths => m_RewardMaxHealths;

        private int[] m_RewardCardNumbers = Array.Empty<int>();
        private int[] m_RewardAttacks = Array.Empty<int>();
        private int[] m_RewardMaxHealths = Array.Empty<int>();

        public override EDataLoad GetDataLoadType() => EDataLoad.Override;

        public override string[] GetTableNames() => new[] { nameof(BattleProgressionCsvData) };

        public PreparationRewardBatchStartupData CreateRewardBatchSnapshot()
        {
            ValidateShapeAndValues();
            var grants = new RewardCardGrantStartupData[RunCardRules.RewardGrantCount];
            for (var index = 0; index < grants.Length; index++)
            {
                var cardNumber = m_RewardCardNumbers[index];
                var card = DataApi.GetData<BattleCardCsvData>(cardNumber)
                    ?? throw new InvalidOperationException(
                        $"Battle progression {BattleNumber} references missing card {cardNumber}.");
                if (DataApi.GetData<BattleCardTypeCsvData>(card.CardTypeId) == null)
                {
                    throw new InvalidOperationException(
                        $"Battle progression {BattleNumber} card {cardNumber} references missing type {card.CardTypeId}.");
                }
                grants[index] = new RewardCardGrantStartupData(
                    cardNumber,
                    m_RewardAttacks[index],
                    m_RewardMaxHealths[index]);
            }
            return new PreparationRewardBatchStartupData(SettlementRewardBatchId, grants);
        }

        protected override void ReadLine()
        {
            BattleNumber = ParseIntFromKey(nameof(BattleNumber));
            SettlementRewardBatchId = GetStringFromKey(nameof(SettlementRewardBatchId));
            m_RewardCardNumbers = ParseIntArrayFromKey(nameof(RewardCardNumbers));
            m_RewardAttacks = ParseIntArrayFromKey(nameof(RewardAttacks));
            m_RewardMaxHealths = ParseIntArrayFromKey(nameof(RewardMaxHealths));
            ValidateShapeAndValues();
            DataApi.SetData(BattleNumber, this);
        }

        private void ValidateShapeAndValues()
        {
            if (BattleNumber <= 0)
                throw new InvalidOperationException("Battle progression number must be positive.");
            if (string.IsNullOrWhiteSpace(SettlementRewardBatchId))
                throw new InvalidOperationException($"Battle progression {BattleNumber} has no settlement reward batch id.");
            if (m_RewardCardNumbers.Length != RunCardRules.RewardGrantCount ||
                m_RewardAttacks.Length != RunCardRules.RewardGrantCount ||
                m_RewardMaxHealths.Length != RunCardRules.RewardGrantCount)
            {
                throw new InvalidOperationException(
                    $"Battle progression {BattleNumber} must define exactly {RunCardRules.RewardGrantCount} rewards and stats.");
            }

            var visited = new HashSet<int>();
            for (var index = 0; index < RunCardRules.RewardGrantCount; index++)
            {
                var cardNumber = m_RewardCardNumbers[index];
                if (cardNumber < RunCardRules.FirstCardNumber || cardNumber > RunCardRules.LastOrdinaryCardNumber)
                    throw new InvalidOperationException($"Battle progression {BattleNumber} reward card {cardNumber} is invalid.");
                if (visited.Add(cardNumber) == false)
                    throw new InvalidOperationException($"Battle progression {BattleNumber} repeats reward card {cardNumber}.");
                if (m_RewardAttacks[index] < 0)
                    throw new InvalidOperationException($"Battle progression {BattleNumber} reward attack cannot be negative.");
                if (m_RewardMaxHealths[index] <= 0)
                    throw new InvalidOperationException($"Battle progression {BattleNumber} reward max health must be positive.");
            }
        }
    }
}
