using System;
using BbxCommon;
using Unity.Entities;

namespace Hearthstone
{
    public enum EBattleResult
    {
        InProgress,
        PlayerVictory,
        EnemyVictory,
    }

    /// <summary>
    /// 一场自动战斗的唯一会话状态。
    /// </summary>
    public sealed class BattleSessionSingletonRawComponent : EcsSingletonRawComponent
    {
        public readonly Entity[] PlayerCards = new Entity[BattleRules.CardsPerSide];
        public readonly Entity[] EnemyCards = new Entity[BattleRules.CardsPerSide];
        public int PlayerAttackCursor;
        public int EnemyAttackCursor;
        public readonly ListenableVariable<EBattleSide> CurrentSide = new(EBattleSide.Player);
        public readonly ListenableVariable<EBattleResult> Result = new(EBattleResult.InProgress);
        public readonly ListenableVariable<Entity> CurrentAttacker = new(Entity.Null);
        public readonly ListenableVariable<Entity> CurrentTarget = new(Entity.Null);
        public uint RandomSeed;
        public Unity.Mathematics.Random TargetRandom;
        public float ActionCountdown;
        public int ActionIndex;

        public void Initialize(uint randomSeed)
        {
            Array.Clear(PlayerCards, 0, PlayerCards.Length);
            Array.Clear(EnemyCards, 0, EnemyCards.Length);
            PlayerAttackCursor = 0;
            EnemyAttackCursor = 0;
            CurrentSide.SetValue(EBattleSide.Player);
            Result.SetValue(EBattleResult.InProgress);
            CurrentAttacker.SetValue(Entity.Null);
            CurrentTarget.SetValue(Entity.Null);
            RandomSeed = BattleRules.NormalizeSeed(randomSeed);
            TargetRandom = new Unity.Mathematics.Random(RandomSeed);
            ActionCountdown = BattleRules.ActionInterval;
            ActionIndex = 0;
        }

        public Entity[] GetCards(EBattleSide side)
        {
            return side == EBattleSide.Player ? PlayerCards : EnemyCards;
        }

        public int GetAttackCursor(EBattleSide side)
        {
            return side == EBattleSide.Player ? PlayerAttackCursor : EnemyAttackCursor;
        }

        public void SetAttackCursor(EBattleSide side, int cursor)
        {
            if (side == EBattleSide.Player)
                PlayerAttackCursor = cursor;
            else
                EnemyAttackCursor = cursor;
        }

        protected override void OnSingletonCollect()
        {
            CurrentSide.MakeInvalid();
            Result.MakeInvalid();
            CurrentAttacker.MakeInvalid();
            CurrentTarget.MakeInvalid();
            Array.Clear(PlayerCards, 0, PlayerCards.Length);
            Array.Clear(EnemyCards, 0, EnemyCards.Length);
            PlayerAttackCursor = 0;
            EnemyAttackCursor = 0;
            CurrentSide.SetValue(EBattleSide.Player);
            Result.SetValue(EBattleResult.InProgress);
            CurrentAttacker.SetValue(Entity.Null);
            CurrentTarget.SetValue(Entity.Null);
            RandomSeed = 0;
            TargetRandom = default;
            ActionCountdown = 0f;
            ActionIndex = 0;
        }
    }
}
