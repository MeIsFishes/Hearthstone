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
        public int MaxHealth;
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
            Attack = typeConfig.RollAttack(ref random);
            MaxHealth = typeConfig.RollHealth(ref random);
            CurrentHealth.SetValue(MaxHealth);
            IsAlive.SetValue(true);
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
            Attack = instance.Attack;
            MaxHealth = instance.MaxHealth;
            CurrentHealth.SetValue(MaxHealth);
            IsAlive.SetValue(true);
        }

        public void SetCurrentHealth(int health)
        {
            var clampedHealth = health;
            if (clampedHealth < 0)
                clampedHealth = 0;
            else if (clampedHealth > MaxHealth)
                clampedHealth = MaxHealth;

            CurrentHealth.SetValue(clampedHealth);
            IsAlive.SetValue(clampedHealth > 0);
        }

        protected override void OnComponentCollect()
        {
            CurrentHealth.MakeInvalid();
            IsAlive.MakeInvalid();
            CardNumber = 0;
            CardTypeId = 0;
            Side = EBattleSide.Player;
            SlotIndex = 0;
            Attack = 0;
            MaxHealth = 0;
            CurrentHealth.SetValue(0);
            IsAlive.SetValue(false);
        }
    }
}
