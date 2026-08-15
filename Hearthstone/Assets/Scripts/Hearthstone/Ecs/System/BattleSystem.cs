using BbxCommon;
using Unity.Entities;

namespace Hearthstone
{
    /// <summary>
    /// 驱动敌我交替进行的核心自动战斗。
    /// </summary>
    [DisableAutoCreation]
    public partial class BattleSystem : EcsMixSystemBase
    {
        protected override void OnSystemUpdate()
        {
            var session = GetSingletonRawComponent<BattleSessionSingletonRawComponent>();
            if (session == null)
                return;
            if (session.Result.Value != EBattleResult.InProgress)
            {
                AdvanceOutcomePresentation(session);
                return;
            }
            if (session.ResultSettlementPending)
            {
                AdvanceResultSettlement(session);
                return;
            }

            if (session.AttackPresentationActive)
            {
                AdvanceAttackPresentation(session);
                return;
            }

            session.ActionCountdown -= TimeApi.DeltaTime;
            if (session.ActionCountdown > 0f)
                return;

            ExecuteAction(session);
        }

        private static void ExecuteAction(BattleSessionSingletonRawComponent session)
        {
            var playerAliveMask = BuildAliveMask(session.PlayerCards);
            var enemyAliveMask = BuildAliveMask(session.EnemyCards);
            var result = BattleRules.EvaluateResult(playerAliveMask, enemyAliveMask);
            if (result != EBattleResult.InProgress)
            {
                BeginResultSettlement(session, result);
                return;
            }

            var actingSide = session.CurrentSide.Value;
            var attackerCards = session.GetCards(actingSide);
            var targetCards = session.GetCards(BattleRules.GetOppositeSide(actingSide));
            var attackerMask = actingSide == EBattleSide.Player ? playerAliveMask : enemyAliveMask;
            var targetMask = actingSide == EBattleSide.Player ? enemyAliveMask : playerAliveMask;
            var attackerSlot = BattleRules.FindNextLivingSlot(
                session.GetAttackCursor(actingSide),
                attackerMask,
                attackerCards.Length);
            if (attackerSlot < 0)
            {
                BeginResultSettlement(session, BattleRules.EvaluateResult(playerAliveMask, enemyAliveMask));
                return;
            }

            var attackerEntity = attackerCards[attackerSlot];
            var attacker = attackerEntity.GetRawComponent<BattleCardRawComponent>();
            if (BattleKeywordRules.Has(attacker.Keywords, EBattleKeyword.Charge))
                ApplyCharge(attackerCards);

            var tauntMask = BuildKeywordMask(targetCards, EBattleKeyword.Taunt);
            var candidateMask = BattleRules.FilterTargetCandidateMask(targetMask, tauntMask);
            var targetOrdinal = session.TargetRandom.NextInt(BattleRules.CountLiving(candidateMask));
            var targetSlot = BattleRules.SelectLivingSlot(candidateMask, targetOrdinal);
            var targetEntity = targetCards[targetSlot];
            var target = targetEntity.GetRawComponent<BattleCardRawComponent>();

            DebugApi.Log(
                $"[BattleKeyword] TargetSelection Action={session.ActionIndex + 1} " +
                $"AttackerSlot={attackerSlot} AttackerCard={attacker.CardNumber} " +
                $"TauntMask=0x{tauntMask:X} CandidateMask=0x{candidateMask:X} TargetSlot={targetSlot}");

            session.SetAttackCursor(actingSide, BattleRules.GetNextCursor(attackerSlot, attackerCards.Length));
            session.CurrentAttacker.SetValue(attackerEntity);
            session.CurrentTarget.SetValue(targetEntity);

            var damage = BattleRules.ResolveKeywordDamage(attacker.Attack, target.Attack, attacker.Keywords);
            var blastDistance = BattleKeywordRules.Has(attacker.Keywords, EBattleKeyword.Blast)
                ? BattleKeywordRules.GetConfig(EBattleKeyword.Blast).BlastDistance
                : 0;
            var adjacentMask = damage.BlastDamage > 0
                ? BattleRules.GetAdjacentLivingMask(targetSlot, targetMask, blastDistance, targetCards.Length)
                : 0u;
            var attackerHealthBefore = attacker.CurrentHealth.Value;
            var targetHealthBefore = target.CurrentHealth.Value;
            var presentationConfig = DataApi.GetData<BattleCardTypeCsvData>(attacker.PresentationCardTypeId);
            session.AttackPresentationActive = true;
            session.AttackPresentationElapsed = 0f;
            session.AttackPresentationDuration = BattleRules.GetAttackPresentationDuration(presentationConfig);
            session.PendingHitDelays = presentationConfig?.HitDelays ?? System.Array.Empty<float>();
            session.PendingAttackAudioKeys = presentationConfig?.AttackAudioKeys ?? System.Array.Empty<string>();
            session.PendingAttackAudioDelays = presentationConfig?.AttackAudioDelays ?? System.Array.Empty<float>();
            session.PendingAttackAudioVolumes = presentationConfig?.AttackAudioVolumes ?? System.Array.Empty<float>();
            session.PendingNextHitIndex = 0;
            session.PendingNextAttackAudioIndex = 0;
            session.PendingDamageApplied = false;
            session.PendingActingSide = actingSide;
            session.PendingAttackerSlot = attackerSlot;
            session.PendingTargetSlot = targetSlot;
            session.PendingAdjacentMask = adjacentMask;
            session.PendingDamage = damage;
            session.PendingAttackerHealthBefore = attackerHealthBefore;
            session.PendingTargetHealthBefore = targetHealthBefore;
            session.AttackPresentationSequence.SetValue(session.ActionIndex + 1);
        }

        private static void AdvanceAttackPresentation(BattleSessionSingletonRawComponent session)
        {
            session.AttackPresentationElapsed +=
                TimeApi.DeltaTime * BattleRules.AttackPresentationPlaybackSpeed;
            while (session.PendingNextAttackAudioIndex < session.PendingAttackAudioDelays.Length &&
                   session.AttackPresentationElapsed >= session.PendingAttackAudioDelays[session.PendingNextAttackAudioIndex])
            {
                PlayPendingAttackAudio(session, session.PendingNextAttackAudioIndex);
                session.PendingNextAttackAudioIndex++;
            }
            while (session.PendingNextHitIndex < session.PendingHitDelays.Length &&
                   session.AttackPresentationElapsed >= session.PendingHitDelays[session.PendingNextHitIndex])
            {
                if (session.PendingDamageApplied == false)
                    ApplyPendingDamage(session);
                session.PendingNextHitIndex++;
            }

            if (session.AttackPresentationElapsed < session.AttackPresentationDuration)
                return;

            if (session.PendingDamageApplied == false)
                ApplyPendingDamage(session);
            CompleteAttackPresentation(session);
        }

        private static void PlayPendingAttackAudio(BattleSessionSingletonRawComponent session, int audioIndex)
        {
            var audioKey = session.PendingAttackAudioKeys[audioIndex];
            if (string.IsNullOrWhiteSpace(audioKey))
                return;

            var options = AudioPlayOptions.Default;
            options.Volume = session.PendingAttackAudioVolumes[audioIndex];
            options.Priority = 96;
            options.GroupKey = "Combat";
            options.ConcurrencyKey = "BattleCardAttack";
            options.MaxConcurrent = 3;
            options.ConcurrencyVolumeFalloff = 0.72f;
            AudioApi.Play(audioKey, options);
        }

        private static void ApplyPendingDamage(BattleSessionSingletonRawComponent session)
        {
            session.PendingDamageApplied = true;
            var attackerEntity = session.CurrentAttacker.Value;
            var targetEntity = session.CurrentTarget.Value;
            var attacker = attackerEntity == Entity.Null
                ? null
                : attackerEntity.GetRawComponent<BattleCardRawComponent>();
            var target = targetEntity == Entity.Null
                ? null
                : targetEntity.GetRawComponent<BattleCardRawComponent>();
            if (attacker == null || target == null)
                return;

            attacker.SetCurrentHealthWithoutAliveCommit(
                session.PendingAttackerHealthBefore - session.PendingDamage.CounterDamage);
            target.SetCurrentHealthWithoutAliveCommit(
                session.PendingTargetHealthBefore - session.PendingDamage.MainDamage);

            var targetCards = session.GetCards(BattleRules.GetOppositeSide(session.PendingActingSide));
            for (var slot = 0; slot < targetCards.Length; slot++)
            {
                if ((session.PendingAdjacentMask & (1u << slot)) == 0)
                    continue;
                var adjacent = targetCards[slot].GetRawComponent<BattleCardRawComponent>();
                if (adjacent == null)
                    continue;
                var adjacentHealthBefore = adjacent.CurrentHealth.Value;
                adjacent.SetCurrentHealthWithoutAliveCommit(adjacentHealthBefore - session.PendingDamage.BlastDamage);
                DebugApi.Log(
                    $"[BattleKeyword] AdjacentDamage Action={session.ActionIndex + 1} " +
                    $"Slot={slot} Card={adjacent.CardNumber} Damage={session.PendingDamage.BlastDamage} " +
                    $"Health={adjacentHealthBefore}->{adjacent.CurrentHealth.Value}");
            }
        }

        private static void CompleteAttackPresentation(BattleSessionSingletonRawComponent session)
        {
            var attackerEntity = session.CurrentAttacker.Value;
            var targetEntity = session.CurrentTarget.Value;
            var attacker = attackerEntity == Entity.Null
                ? null
                : attackerEntity.GetRawComponent<BattleCardRawComponent>();
            var target = targetEntity == Entity.Null
                ? null
                : targetEntity.GetRawComponent<BattleCardRawComponent>();
            var targetCards = session.GetCards(BattleRules.GetOppositeSide(session.PendingActingSide));

            attacker?.CommitAliveState();
            target?.CommitAliveState();
            for (var slot = 0; slot < targetCards.Length; slot++)
            {
                if ((session.PendingAdjacentMask & (1u << slot)) != 0)
                    targetCards[slot].GetRawComponent<BattleCardRawComponent>()?.CommitAliveState();
            }

            session.ActionIndex++;
            if (attacker != null && target != null)
            {
                DebugApi.Log(
                    $"[BattleKeyword] Action={session.ActionIndex} Side={session.PendingActingSide} " +
                    $"AttackerSlot={session.PendingAttackerSlot} AttackerCard={attacker.CardNumber} Keywords={attacker.Keywords} " +
                    $"TargetSlot={session.PendingTargetSlot} TargetCard={target.CardNumber} " +
                    $"MainDamage={session.PendingDamage.MainDamage} BlastDamage={session.PendingDamage.BlastDamage} " +
                    $"CounterDamage={session.PendingDamage.CounterDamage} AdjacentMask=0x{session.PendingAdjacentMask:X} " +
                    $"AttackerHealth={session.PendingAttackerHealthBefore}->{attacker.CurrentHealth.Value} " +
                    $"TargetHealth={session.PendingTargetHealthBefore}->{target.CurrentHealth.Value}");
            }

            var playerAliveMask = BuildAliveMask(session.PlayerCards);
            var enemyAliveMask = BuildAliveMask(session.EnemyCards);
            var result = BattleRules.EvaluateResult(playerAliveMask, enemyAliveMask);
            DebugApi.Log(
                $"[BattleKeyword] Result Action={session.ActionIndex} " +
                $"PlayerAliveMask=0x{playerAliveMask:X} EnemyAliveMask=0x{enemyAliveMask:X} Result={result}");

            var actingSide = session.PendingActingSide;
            session.ClearPendingAttackPresentation();
            session.CurrentAttacker.SetValue(Entity.Null);
            session.CurrentTarget.SetValue(Entity.Null);
            session.ActionCountdown = BattleRules.AttackEndWaitDuration;
            if (result == EBattleResult.InProgress)
                session.CurrentSide.SetValue(BattleRules.GetOppositeSide(actingSide));
            else
                BeginResultSettlement(session, result);
        }

        private static void BeginResultSettlement(
            BattleSessionSingletonRawComponent session,
            EBattleResult result)
        {
            if (result == EBattleResult.InProgress || session.ResultSettlementPending)
                return;
            session.ResultSettlementPending = true;
            session.PendingResult = result;
            session.ResultSettlementCountdown = BattleRules.ResultSettlementDelay;
        }

        private static void AdvanceResultSettlement(BattleSessionSingletonRawComponent session)
        {
            session.ResultSettlementCountdown -= TimeApi.DeltaTime;
            if (session.ResultSettlementCountdown > 0f)
                return;
            session.ResultSettlementPending = false;
            session.ResultSettlementCountdown = 0f;
            var result = session.PendingResult;
            session.PendingResult = EBattleResult.InProgress;
            if (result == EBattleResult.PlayerVictory)
                session.OutcomePresentationCountdown = BattleRules.VictoryBannerTotalDuration;
            session.Result.SetValue(result);
        }

        private static void AdvanceOutcomePresentation(BattleSessionSingletonRawComponent session)
        {
            if (session.Result.Value != EBattleResult.PlayerVictory ||
                session.OutcomePresentationCompleted.Value)
                return;
            session.OutcomePresentationCountdown -= TimeApi.DeltaTime;
            if (session.OutcomePresentationCountdown > 0f)
                return;
            session.OutcomePresentationCountdown = 0f;
            session.OutcomePresentationCompleted.SetValue(true);
        }

        private static void ApplyCharge(Entity[] cards)
        {
            var charge = BattleKeywordRules.GetConfig(EBattleKeyword.Charge);
            for (var slot = 0; slot < cards.Length; slot++)
            {
                var entity = cards[slot];
                if (entity == Entity.Null)
                    continue;
                var card = entity.GetRawComponent<BattleCardRawComponent>();
                if (card == null || card.IsAlive.Value == false)
                    continue;
                var attackBefore = card.Attack;
                var healthBefore = card.CurrentHealth.Value;
                card.ApplyBattleStatGain(charge.AttackGain, charge.HealthGain);
                DebugApi.Log(
                    $"[BattleKeyword] Charge Side={card.Side} Slot={slot} Card={card.CardNumber} " +
                    $"Attack={attackBefore}->{card.Attack} Health={healthBefore}->{card.CurrentHealth.Value} " +
                    $"MaxHealth={card.MaxHealth - charge.HealthGain}->{card.MaxHealth}");
            }
        }

        private static uint BuildAliveMask(Entity[] cards)
        {
            var aliveMask = 0u;
            for (var slot = 0; slot < cards.Length; slot++)
            {
                var entity = cards[slot];
                if (entity == Entity.Null)
                    continue;
                var card = entity.GetRawComponent<BattleCardRawComponent>();
                if (card != null && card.IsAlive.Value)
                    aliveMask |= 1u << slot;
            }
            return aliveMask;
        }

        private static uint BuildKeywordMask(Entity[] cards, EBattleKeyword keyword)
        {
            var mask = 0u;
            for (var slot = 0; slot < cards.Length; slot++)
            {
                var entity = cards[slot];
                if (entity == Entity.Null)
                    continue;
                var card = entity.GetRawComponent<BattleCardRawComponent>();
                if (card != null && card.IsAlive.Value && BattleKeywordRules.Has(card.Keywords, keyword))
                    mask |= 1u << slot;
            }
            return mask;
        }
    }
}
