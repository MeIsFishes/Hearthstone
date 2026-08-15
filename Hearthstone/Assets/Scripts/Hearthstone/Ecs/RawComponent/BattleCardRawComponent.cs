using System;
using BbxCommon;
using Random = Unity.Mathematics.Random;

namespace Hearthstone
{
    public enum EBattleSide
    {
        Player,
        Enemy,
    }

    /// <summary>
    /// 单张战斗卡牌的运行时状态。
    /// </summary>
    public sealed class BattleCardRawComponent : EcsRawComponent
    {
        public int CardNumber;
        public int CardTypeId;
        public EBattleSide Side;
        public int SlotIndex;
        public int Attack;
        public int EntryAttack;
        public EBattleKeyword Keywords;
        public EBattleCardTier Tier;
        public int MaxHealth;
        public int EntryHealth;
        public readonly ListenableVariable<int> AttackValue = new(0);
        public readonly ListenableVariable<int> CurrentHealth = new(0);
        public readonly ListenableVariable<bool> IsAlive = new(false);

        public void Initialize(
            EBattleSide side,
            int slotIndex,
            BattleCardCsvData cardConfig,
            BattleCardTypeCsvData typeConfig,
            ref Random random)
        {
            if (cardConfig == null)
                throw new ArgumentNullException(nameof(cardConfig));
            if (typeConfig == null)
                throw new ArgumentNullException(nameof(typeConfig));
            if (cardConfig.CardTypeId != typeConfig.CardTypeId)
                throw new ArgumentException("Card and type configurations do not reference the same card type.");

            CardNumber = cardConfig.CardNumber;
            CardTypeId = cardConfig.CardTypeId;
            Side = side;
            SlotIndex = slotIndex;
            Tier = typeConfig.Tier;
            var attack = typeConfig.RollAttack(ref random);
            var maxHealth = typeConfig.RollHealth(ref random);
            InitializeValues(attack, maxHealth, maxHealth, typeConfig.InitialKeyword);
        }

        public void InitializePlayer(int slotIndex, RunCardInstanceData instance)
        {
            if (instance.IsValid == false)
                throw new ArgumentException("Player card instance is invalid.", nameof(instance));

            CardNumber = instance.CardNumber;
            var cardConfig = DataApi.GetData<BattleCardCsvData>(CardNumber);
            if (cardConfig == null)
                throw new InvalidOperationException($"Battle card configuration {CardNumber} is missing.");
            CardTypeId = cardConfig.CardTypeId;
            Side = EBattleSide.Player;
            SlotIndex = slotIndex;
            Tier = instance.Tier;
            InitializeValues(instance.Attack, instance.MaxHealth, instance.MaxHealth, instance.Keywords);
        }

        public void InitializePlayerExplicit(
            int slotIndex,
            RunCardInstanceData instance,
            int attack,
            int maxHealth,
            int currentHealth)
        {
            InitializePlayer(slotIndex, instance);
            InitializeValues(attack, maxHealth, currentHealth, instance.Keywords);
        }

        public void InitializeExplicit(
            EBattleSide side,
            int slotIndex,
            BattleCardCsvData cardConfig,
            BattleCardTypeCsvData typeConfig,
            int attack,
            int maxHealth,
            int currentHealth)
        {
            if (cardConfig == null)
                throw new ArgumentNullException(nameof(cardConfig));
            if (typeConfig == null)
                throw new ArgumentNullException(nameof(typeConfig));
            if (cardConfig.CardTypeId != typeConfig.CardTypeId)
                throw new ArgumentException("Card and type configurations do not reference the same card type.");
            CardNumber = cardConfig.CardNumber;
            CardTypeId = cardConfig.CardTypeId;
            Side = side;
            SlotIndex = slotIndex;
            Tier = typeConfig.Tier;
            InitializeValues(attack, maxHealth, currentHealth, typeConfig.InitialKeyword);
        }

        public void SetAttack(int attack)
        {
            if (attack < 0)
                throw new ArgumentOutOfRangeException(nameof(attack));
            Attack = attack;
            AttackValue.SetValue(attack);
        }

        public void SyncAttackValue()
        {
            AttackValue.SetValue(Attack);
        }

        public void ApplyBattleStatGain(int attackGain, int healthGain)
        {
            if (attackGain < 0)
                throw new ArgumentOutOfRangeException(nameof(attackGain));
            if (healthGain < 0)
                throw new ArgumentOutOfRangeException(nameof(healthGain));
            if (IsAlive.Value == false)
                return;
            SetAttack(checked(Attack + attackGain));
            MaxHealth = checked(MaxHealth + healthGain);
            CurrentHealth.SetValue(checked(CurrentHealth.Value + healthGain));
        }

        public void SetCurrentHealth(int health)
        {
            SetCurrentHealthWithoutAliveCommit(health);
            CommitAliveState();
        }

        public void SetCurrentHealthWithoutAliveCommit(int health)
        {
            CurrentHealth.SetValue(Math.Max(0, Math.Min(health, MaxHealth)));
        }

        public void CommitAliveState()
        {
            IsAlive.SetValue(CurrentHealth.Value > 0);
        }

        private void InitializeValues(
            int attack,
            int maxHealth,
            int currentHealth,
            EBattleKeyword keywords)
        {
            if (maxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (currentHealth < 0 || currentHealth > maxHealth)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));
            EntryAttack = attack;
            EntryHealth = currentHealth;
            SetAttack(attack);
            MaxHealth = maxHealth;
            CurrentHealth.SetValue(currentHealth);
            IsAlive.SetValue(currentHealth > 0);
            Keywords = BattleKeywordRules.Normalize(keywords);
        }

        protected override void OnComponentCollect()
        {
            AttackValue.MakeInvalid();
            CurrentHealth.MakeInvalid();
            IsAlive.MakeInvalid();
            CardNumber = 0;
            CardTypeId = 0;
            Side = EBattleSide.Player;
            SlotIndex = 0;
            Attack = 0;
            EntryAttack = 0;
            Keywords = EBattleKeyword.None;
            Tier = EBattleCardTier.Bronze;
            MaxHealth = 0;
            EntryHealth = 0;
            AttackValue.SetValue(0);
            CurrentHealth.SetValue(0);
            IsAlive.SetValue(false);
        }
    }
}
