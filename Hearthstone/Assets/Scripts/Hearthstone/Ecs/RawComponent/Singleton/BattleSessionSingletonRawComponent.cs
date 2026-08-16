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
        public Entity[] PlayerCards = Array.Empty<Entity>();
        public Entity[] EnemyCards = Array.Empty<Entity>();
        public int BattleNumber;
        public bool IsFinalBattle;
        public int PlayerAttackCursor;
        public int EnemyAttackCursor;
        public readonly ListenableVariable<EBattleSide> CurrentSide = new(EBattleSide.Player);
        public readonly ListenableVariable<EBattleResult> Result = new(EBattleResult.InProgress);
        public readonly ListenableVariable<bool> OutcomePresentationCompleted = new(false);
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
        public bool ResultSettlementPending;
        public EBattleResult PendingResult;
        public float ResultSettlementCountdown;
        public float OutcomePresentationCountdown;

        public void Initialize(
            uint randomSeed,
            PreparationRewardBatchStartupData rewardBatch,
            int battleNumber,
            bool isFinalBattle,
            int playerSlotCount,
            int enemySlotCount = BattleRules.CardsPerSide)
        {
            if (rewardBatch == null)
                throw new ArgumentNullException(nameof(rewardBatch));
            if (battleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            if (playerSlotCount < RunCardRules.InitialBattleSlotCount ||
                playerSlotCount > RunCardRules.MaximumBattleSlotCount)
                throw new ArgumentOutOfRangeException(nameof(playerSlotCount));
            if (enemySlotCount <= 0 || enemySlotCount > RunCardRules.MaximumBattleSlotCount)
                throw new ArgumentOutOfRangeException(nameof(enemySlotCount));
            PlayerCards = new Entity[playerSlotCount];
            EnemyCards = new Entity[enemySlotCount];
            BattleNumber = battleNumber;
            IsFinalBattle = isFinalBattle;
            PlayerAttackCursor = 0;
            EnemyAttackCursor = 0;
            CurrentSide.SetValue(EBattleSide.Player);
            Result.SetValue(EBattleResult.InProgress);
            OutcomePresentationCompleted.SetValue(false);
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
            ResultSettlementPending = false;
            PendingResult = EBattleResult.InProgress;
            ResultSettlementCountdown = 0f;
            OutcomePresentationCountdown = 0f;
        }

        public void Initialize(uint randomSeed, PreparationRewardBatchStartupData rewardBatch)
        {
            Initialize(
                randomSeed,
                rewardBatch,
                1,
                false,
                RunCardRules.InitialBattleSlotCount);
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
            OutcomePresentationCompleted.MakeInvalid();
            CurrentAttacker.MakeInvalid();
            CurrentTarget.MakeInvalid();
            AttackPresentationSequence.MakeInvalid();
            PlayerCards = Array.Empty<Entity>();
            EnemyCards = Array.Empty<Entity>();
            BattleNumber = 0;
            IsFinalBattle = false;
            PlayerAttackCursor = 0;
            EnemyAttackCursor = 0;
            CurrentSide.SetValue(EBattleSide.Player);
            Result.SetValue(EBattleResult.InProgress);
            OutcomePresentationCompleted.SetValue(false);
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
            ResultSettlementPending = false;
            PendingResult = EBattleResult.InProgress;
            ResultSettlementCountdown = 0f;
            OutcomePresentationCountdown = 0f;
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
