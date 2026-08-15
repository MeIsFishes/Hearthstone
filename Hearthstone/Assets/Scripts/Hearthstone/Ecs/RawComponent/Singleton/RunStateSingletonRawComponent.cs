using System;
using System.Collections.Generic;
using BbxCommon;

namespace Hearthstone
{
    public readonly struct RunCardInstanceData : IEquatable<RunCardInstanceData>
    {
        public int CardNumber { get; }
        public int Attack { get; }
        public int MaxHealth { get; }
        public EBattleKeyword Keywords { get; }
        public bool IsValid => CardNumber != 0;

        public RunCardInstanceData(
            int cardNumber,
            int attack,
            int maxHealth,
            EBattleKeyword? keywords = null)
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
            Keywords = BattleKeywordRules.Normalize(keywords ?? ResolveInitialKeywords(cardNumber));
        }

        public bool Equals(RunCardInstanceData other)
        {
            return CardNumber == other.CardNumber &&
                   Attack == other.Attack &&
                   MaxHealth == other.MaxHealth &&
                   Keywords == other.Keywords;
        }

        public override bool Equals(object obj)
        {
            return obj is RunCardInstanceData other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = CardNumber;
                hashCode = (hashCode * 397) ^ Attack;
                hashCode = (hashCode * 397) ^ MaxHealth;
                hashCode = (hashCode * 397) ^ (int)Keywords;
                return hashCode;
            }
        }

        private static EBattleKeyword ResolveInitialKeywords(int cardNumber)
        {
            var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
            if (cardConfig == null)
                return EBattleKeyword.None;
            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            return typeConfig == null ? EBattleKeyword.None : typeConfig.InitialKeyword;
        }
    }

    public sealed class RunStateSingletonRawComponent : EcsSingletonRawComponent
    {
        public readonly RunCardInstanceData[] CardInstances = new RunCardInstanceData[RunCardRules.CardStorageLength];
        public readonly int[] BattleSlotCardNumbers = new int[RunCardRules.BattleSlotCount];
        public readonly Dictionary<string, string> AppliedRewardBatchPayloadFingerprints =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly ListenableVariable<int> Revision = new ListenableVariable<int>(0);

        public bool HasCard(int cardNumber)
        {
            return cardNumber >= RunCardRules.FirstCardNumber &&
                   cardNumber <= RunCardRules.LastCardNumber &&
                   CardInstances[cardNumber].IsValid;
        }

        public int GetOwnedCardCount()
        {
            var count = 0;
            for (var cardNumber = RunCardRules.FirstCardNumber; cardNumber <= RunCardRules.LastCardNumber; cardNumber++)
            {
                if (CardInstances[cardNumber].IsValid)
                    count++;
            }
            return count;
        }

        protected override void OnSingletonCollect()
        {
            Revision.MakeInvalid();
            Array.Clear(CardInstances, 0, CardInstances.Length);
            Array.Clear(BattleSlotCardNumbers, 0, BattleSlotCardNumbers.Length);
            AppliedRewardBatchPayloadFingerprints.Clear();
            Revision.SetValue(0);
        }
    }

    public sealed class PreparationSessionSingletonRawComponent : EcsSingletonRawComponent
    {
        public string BatchId;
        public readonly RunCardInstanceData[] RewardCards = new RunCardInstanceData[RunCardRules.RewardGrantCount];
        public readonly int[] FusionSlotCardNumbers = new int[RunCardRules.FusionSlotCount];
        public readonly ListenableVariable<int> FusionRevision = new ListenableVariable<int>(0);
        public bool WasNewlyApplied;

        public void Initialize(PreparationRewardBatchStartupData batch, bool wasNewlyApplied)
        {
            BatchId = batch.BatchId;
            WasNewlyApplied = wasNewlyApplied;
            for (var index = 0; index < RewardCards.Length; index++)
            {
                var grant = batch.Grants[index];
                RewardCards[index] = new RunCardInstanceData(grant.CardNumber, grant.Attack, grant.MaxHealth);
            }
            Array.Clear(FusionSlotCardNumbers, 0, FusionSlotCardNumbers.Length);
            FusionRevision.SetValue(0);
        }

        protected override void OnSingletonCollect()
        {
            FusionRevision.MakeInvalid();
            BatchId = null;
            Array.Clear(RewardCards, 0, RewardCards.Length);
            Array.Clear(FusionSlotCardNumbers, 0, FusionSlotCardNumbers.Length);
            FusionRevision.SetValue(0);
            WasNewlyApplied = false;
        }
    }
}
