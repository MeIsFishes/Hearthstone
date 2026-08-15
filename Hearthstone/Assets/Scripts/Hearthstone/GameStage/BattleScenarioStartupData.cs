using System;

namespace Hearthstone
{
    public enum EBattleCardStatSource
    {
        RunState,
        Explicit,
    }

    public readonly struct BattleCardSlotStartupData : IEquatable<BattleCardSlotStartupData>
    {
        public bool IsOccupied { get; }
        public int CardNumber { get; }
        public EBattleCardStatSource StatSource { get; }
        public int Attack { get; }
        public int MaxHealth { get; }
        public int CurrentHealth { get; }

        public static BattleCardSlotStartupData Empty => default;

        private BattleCardSlotStartupData(
            bool isOccupied,
            int cardNumber,
            EBattleCardStatSource statSource,
            int attack,
            int maxHealth,
            int currentHealth)
        {
            IsOccupied = isOccupied;
            CardNumber = cardNumber;
            StatSource = statSource;
            Attack = attack;
            MaxHealth = maxHealth;
            CurrentHealth = currentHealth;
        }

        public static BattleCardSlotStartupData FromRunState(int cardNumber)
        {
            ValidateCardNumber(cardNumber);
            return new BattleCardSlotStartupData(true, cardNumber, EBattleCardStatSource.RunState, 0, 0, 0);
        }

        public static BattleCardSlotStartupData Explicit(
            int cardNumber,
            int attack,
            int maxHealth,
            int currentHealth)
        {
            ValidateCardNumber(cardNumber);
            if (attack < 0)
                throw new ArgumentOutOfRangeException(nameof(attack));
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (currentHealth < 0 || currentHealth > maxHealth)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));
            return new BattleCardSlotStartupData(
                true,
                cardNumber,
                EBattleCardStatSource.Explicit,
                attack,
                maxHealth,
                currentHealth);
        }

        public bool Equals(BattleCardSlotStartupData other)
        {
            return IsOccupied == other.IsOccupied &&
                   CardNumber == other.CardNumber &&
                   StatSource == other.StatSource &&
                   Attack == other.Attack &&
                   MaxHealth == other.MaxHealth &&
                   CurrentHealth == other.CurrentHealth;
        }

        public override bool Equals(object obj) =>
            obj is BattleCardSlotStartupData other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsOccupied.GetHashCode();
                hashCode = (hashCode * 397) ^ CardNumber;
                hashCode = (hashCode * 397) ^ (int)StatSource;
                hashCode = (hashCode * 397) ^ Attack;
                hashCode = (hashCode * 397) ^ MaxHealth;
                hashCode = (hashCode * 397) ^ CurrentHealth;
                return hashCode;
            }
        }

        private static void ValidateCardNumber(int cardNumber)
        {
            if (cardNumber < RunCardRules.FirstCardNumber || cardNumber > RunCardRules.LastCardNumber)
                throw new ArgumentOutOfRangeException(nameof(cardNumber));
        }
    }

    public sealed class BattleScenarioStartupData
    {
        private readonly BattleCardSlotStartupData[] m_PlayerSlots;
        private readonly BattleCardSlotStartupData[] m_EnemySlots;

        public uint RandomSeed { get; }
        public int SlotCount => BattleRules.CardsPerSide;

        public BattleScenarioStartupData(
            BattleCardSlotStartupData[] playerSlots,
            BattleCardSlotStartupData[] enemySlots,
            uint randomSeed)
        {
            ValidateSlots(playerSlots, nameof(playerSlots), true);
            ValidateSlots(enemySlots, nameof(enemySlots), false);
            if (randomSeed == 0)
                throw new ArgumentOutOfRangeException(nameof(randomSeed));
            m_PlayerSlots = (BattleCardSlotStartupData[])playerSlots.Clone();
            m_EnemySlots = (BattleCardSlotStartupData[])enemySlots.Clone();
            RandomSeed = randomSeed;
        }

        public BattleCardSlotStartupData GetPlayerSlot(int slot) => GetSlot(m_PlayerSlots, slot);

        public BattleCardSlotStartupData GetEnemySlot(int slot) => GetSlot(m_EnemySlots, slot);

        public BattleScenarioStartupData CreateSnapshot()
        {
            return new BattleScenarioStartupData(m_PlayerSlots, m_EnemySlots, RandomSeed);
        }

        private static BattleCardSlotStartupData GetSlot(BattleCardSlotStartupData[] slots, int slot)
        {
            if (slot < 0 || slot >= slots.Length)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return slots[slot];
        }

        private static void ValidateSlots(
            BattleCardSlotStartupData[] slots,
            string parameterName,
            bool isPlayer)
        {
            if (slots == null)
                throw new ArgumentNullException(parameterName);
            if (slots.Length != BattleRules.CardsPerSide)
                throw new ArgumentException($"A battle scenario must contain exactly {BattleRules.CardsPerSide} slots per side.", parameterName);
            for (var slot = 0; slot < slots.Length; slot++)
            {
                var data = slots[slot];
                if (data.IsOccupied == false)
                {
                    if (data.CardNumber != 0 || data.Attack != 0 || data.MaxHealth != 0 || data.CurrentHealth != 0)
                        throw new ArgumentException($"Empty {parameterName} slot {slot} must use BattleCardSlotStartupData.Empty.", parameterName);
                    continue;
                }
                if (isPlayer == false && data.StatSource != EBattleCardStatSource.Explicit)
                    throw new ArgumentException($"Enemy {parameterName} slot {slot} must use explicit battle stats.", parameterName);
            }
        }
    }
}
