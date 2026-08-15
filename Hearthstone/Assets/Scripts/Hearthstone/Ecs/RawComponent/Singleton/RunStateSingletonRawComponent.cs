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
        public EBattleCardTier Tier { get; }
        public int PresentationCardNumber { get; }
        public bool IsValid => CardNumber != 0;

        public RunCardInstanceData(
            int cardNumber,
            int attack,
            int maxHealth,
            EBattleKeyword? keywords = null,
            EBattleCardTier? tier = null,
            int? presentationCardNumber = null)
        {
            if (cardNumber < RunCardRules.FirstCardNumber || cardNumber > RunCardRules.LastCardNumber)
                throw new ArgumentOutOfRangeException(nameof(cardNumber));
            if (attack < 0)
                throw new ArgumentOutOfRangeException(nameof(attack));
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            var resolvedPresentationCardNumber = presentationCardNumber ?? cardNumber;
            if (resolvedPresentationCardNumber < RunCardRules.FirstCardNumber ||
                resolvedPresentationCardNumber > RunCardRules.LastCardNumber)
                throw new ArgumentOutOfRangeException(nameof(presentationCardNumber));
            CardNumber = cardNumber;
            Attack = attack;
            MaxHealth = maxHealth;
            Keywords = BattleKeywordRules.Normalize(keywords ?? ResolveInitialKeywords(cardNumber));
            Tier = tier ?? ResolveDefaultTier(cardNumber);
            PresentationCardNumber = resolvedPresentationCardNumber;
        }

        public bool Equals(RunCardInstanceData other)
        {
            return CardNumber == other.CardNumber &&
                   Attack == other.Attack &&
                   MaxHealth == other.MaxHealth &&
                   Keywords == other.Keywords &&
                   Tier == other.Tier &&
                   PresentationCardNumber == other.PresentationCardNumber;
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
                hashCode = (hashCode * 397) ^ (int)Tier;
                hashCode = (hashCode * 397) ^ PresentationCardNumber;
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

        private static EBattleCardTier ResolveDefaultTier(int cardNumber)
        {
            var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber);
            if (cardConfig == null)
                return EBattleCardTier.Bronze;
            var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId);
            return typeConfig == null ? EBattleCardTier.Bronze : typeConfig.Tier;
        }
    }

    public sealed class RunStateSingletonRawComponent : EcsSingletonRawComponent
    {
        public readonly RunCardInstanceData[] CardInstances = new RunCardInstanceData[RunCardRules.CardStorageLength];
        public readonly int[] BattleSlotCardNumbers = new int[RunCardRules.MaximumBattleSlotCount];
        public readonly Dictionary<string, string> AppliedRewardBatchPayloadFingerprints =
            new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly ListenableVariable<int> Revision = new ListenableVariable<int>(0);
        private readonly Dictionary<int, List<RunCardInstanceData>> m_AdditionalCardInstances =
            new Dictionary<int, List<RunCardInstanceData>>();

        public int UnlockedBattleSlotCount { get; private set; }

        public void SetUnlockedBattleSlotCount(int slotCount)
        {
            if (slotCount < RunCardRules.InitialBattleSlotCount ||
                slotCount > RunCardRules.MaximumBattleSlotCount)
                throw new ArgumentOutOfRangeException(nameof(slotCount));
            if (UnlockedBattleSlotCount != 0 && slotCount < UnlockedBattleSlotCount)
                throw new InvalidOperationException("Unlocked battle slots cannot decrease during a run.");
            if (UnlockedBattleSlotCount == slotCount)
                return;
            UnlockedBattleSlotCount = slotCount;
            Revision.SetValue(Revision.Value + 1);
        }

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
            foreach (var copies in m_AdditionalCardInstances.Values)
                count += copies.Count;
            return count;
        }

        public int GetCardCopyCount(int cardNumber)
        {
            if (HasCard(cardNumber) == false)
                return 0;
            return 1 + (m_AdditionalCardInstances.TryGetValue(cardNumber, out var copies)
                ? copies.Count
                : 0);
        }

        public RunCardInstanceData GetCardInstance(int cardNumber, int copyIndex = 0)
        {
            if (copyIndex < 0 || HasCard(cardNumber) == false)
                return default;
            if (copyIndex == 0)
                return CardInstances[cardNumber];
            return m_AdditionalCardInstances.TryGetValue(cardNumber, out var copies) &&
                   copyIndex <= copies.Count
                ? copies[copyIndex - 1]
                : default;
        }

        internal void AddCardInstance(RunCardInstanceData instance)
        {
            if (instance.IsValid == false)
                throw new ArgumentException("Cannot add an invalid run card instance.", nameof(instance));
            if (CardInstances[instance.CardNumber].IsValid == false)
            {
                CardInstances[instance.CardNumber] = instance;
                return;
            }
            if (m_AdditionalCardInstances.TryGetValue(instance.CardNumber, out var copies) == false)
            {
                copies = new List<RunCardInstanceData>();
                m_AdditionalCardInstances.Add(instance.CardNumber, copies);
            }
            copies.Add(instance);
        }

        internal bool RemoveCardInstance(int cardNumber, out RunCardInstanceData removed)
        {
            removed = GetCardInstance(cardNumber);
            if (removed.IsValid == false)
                return false;
            if (m_AdditionalCardInstances.TryGetValue(cardNumber, out var copies) && copies.Count > 0)
            {
                CardInstances[cardNumber] = copies[0];
                copies.RemoveAt(0);
                if (copies.Count == 0)
                    m_AdditionalCardInstances.Remove(cardNumber);
            }
            else
            {
                CardInstances[cardNumber] = default;
            }
            return true;
        }

        protected override void OnSingletonCollect()
        {
            Revision.MakeInvalid();
            Array.Clear(CardInstances, 0, CardInstances.Length);
            Array.Clear(BattleSlotCardNumbers, 0, BattleSlotCardNumbers.Length);
            UnlockedBattleSlotCount = 0;
            m_AdditionalCardInstances.Clear();
            AppliedRewardBatchPayloadFingerprints.Clear();
            Revision.SetValue(0);
        }
    }

    public sealed class PreparationSessionSingletonRawComponent : EcsSingletonRawComponent
    {
        public int BattleNumber;
        public string BatchId;
        public RunCardInstanceData[] RewardCards = Array.Empty<RunCardInstanceData>();
        public readonly int[] FusionSlotCardNumbers = new int[RunCardRules.FusionSlotCount];
        public readonly ListenableVariable<int> FusionRevision = new ListenableVariable<int>(0);
        public bool WasNewlyApplied;

        public void Initialize(PreparationRoundStartupData round, bool wasNewlyApplied)
        {
            var batch = round.RewardBatch;
            BattleNumber = round.BattleNumber;
            BatchId = batch.BatchId;
            WasNewlyApplied = wasNewlyApplied;
            RewardCards = new RunCardInstanceData[batch.Grants.Count];
            for (var index = 0; index < RewardCards.Length; index++)
            {
                var grant = batch.Grants[index];
                RewardCards[index] = new RunCardInstanceData(grant.CardNumber, grant.Attack, grant.MaxHealth);
            }
            Array.Clear(FusionSlotCardNumbers, 0, FusionSlotCardNumbers.Length);
            FusionRevision.SetValue(0);
        }

        public void Initialize(PreparationRewardBatchStartupData batch, bool wasNewlyApplied)
        {
            Initialize(
                new PreparationRoundStartupData(
                    1,
                    RunCardRules.InitialBattleSlotCount,
                    batch),
                wasNewlyApplied);
        }

        protected override void OnSingletonCollect()
        {
            FusionRevision.MakeInvalid();
            BattleNumber = 0;
            BatchId = null;
            RewardCards = Array.Empty<RunCardInstanceData>();
            Array.Clear(FusionSlotCardNumbers, 0, FusionSlotCardNumbers.Length);
            FusionRevision.SetValue(0);
            WasNewlyApplied = false;
        }
    }
}
