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
        public readonly ListenableVariable<int> AttackPresentationSequence = new(0);
        public uint RandomSeed;
        public Unity.Mathematics.Random TargetRandom;
        public float ActionCountdown;
        public int ActionIndex;
        public bool AttackPresentationActive;
        public float AttackPresentationElapsed;
        public float AttackPresentationDuration;
        public float[] PendingHitDelays = Array.Empty<float>();
        public string[] PendingAttackAudioKeys = Array.Empty<string>();
        public float[] PendingAttackAudioDelays = Array.Empty<float>();
        public float[] PendingAttackAudioVolumes = Array.Empty<float>();
        public int PendingNextHitIndex;
        public int PendingNextAttackAudioIndex;
        public bool PendingDamageApplied;
        public EBattleSide PendingActingSide;
        public int PendingAttackerSlot;
        public int PendingTargetSlot;
        public uint PendingAdjacentMask;
        public BattleAttackDamageData PendingDamage;
        public int PendingAttackerHealthBefore;
        public int PendingTargetHealthBefore;
        public PreparationRewardBatchStartupData PendingPreparationRewardBatch;
        public bool PreparationTransitionRequested;

        public void Initialize(uint randomSeed, PreparationRewardBatchStartupData rewardBatch)
        {
            if (rewardBatch == null)
                throw new ArgumentNullException(nameof(rewardBatch));
            Array.Clear(PlayerCards, 0, PlayerCards.Length);
            Array.Clear(EnemyCards, 0, EnemyCards.Length);
            PlayerAttackCursor = 0;
            EnemyAttackCursor = 0;
            CurrentSide.SetValue(EBattleSide.Player);
            Result.SetValue(EBattleResult.InProgress);
            CurrentAttacker.SetValue(Entity.Null);
            CurrentTarget.SetValue(Entity.Null);
            AttackPresentationSequence.SetValue(0);
            RandomSeed = BattleRules.NormalizeSeed(randomSeed);
            TargetRandom = new Unity.Mathematics.Random(RandomSeed);
            ActionCountdown = BattleRules.ActionInterval;
            ActionIndex = 0;
            ClearPendingAttackPresentation();
            PendingPreparationRewardBatch = rewardBatch.CreateSnapshot();
            PreparationTransitionRequested = false;
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
            AttackPresentationSequence.MakeInvalid();
            Array.Clear(PlayerCards, 0, PlayerCards.Length);
            Array.Clear(EnemyCards, 0, EnemyCards.Length);
            PlayerAttackCursor = 0;
            EnemyAttackCursor = 0;
            CurrentSide.SetValue(EBattleSide.Player);
            Result.SetValue(EBattleResult.InProgress);
            CurrentAttacker.SetValue(Entity.Null);
            CurrentTarget.SetValue(Entity.Null);
            AttackPresentationSequence.SetValue(0);
            RandomSeed = 0;
            TargetRandom = default;
            ActionCountdown = 0f;
            ActionIndex = 0;
            ClearPendingAttackPresentation();
            PendingPreparationRewardBatch = null;
            PreparationTransitionRequested = false;
        }

        public void ClearPendingAttackPresentation()
        {
            AttackPresentationActive = false;
            AttackPresentationElapsed = 0f;
            AttackPresentationDuration = 0f;
            PendingHitDelays = Array.Empty<float>();
            PendingAttackAudioKeys = Array.Empty<string>();
            PendingAttackAudioDelays = Array.Empty<float>();
            PendingAttackAudioVolumes = Array.Empty<float>();
            PendingNextHitIndex = 0;
            PendingNextAttackAudioIndex = 0;
            PendingDamageApplied = false;
            PendingActingSide = EBattleSide.Player;
            PendingAttackerSlot = -1;
            PendingTargetSlot = -1;
            PendingAdjacentMask = 0u;
            PendingDamage = default;
            PendingAttackerHealthBefore = 0;
            PendingTargetHealthBefore = 0;
        }
    }
}
