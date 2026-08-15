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
            if (session == null || BattleRules.CanAct(session.Result.Value) == false)
                return;

            session.ActionCountdown -= TimeApi.DeltaTime;
            if (session.ActionCountdown > 0f)
                return;

            session.ActionCountdown = BattleRules.ActionInterval;
            ExecuteAction(session);
        }

        private static void ExecuteAction(BattleSessionSingletonRawComponent session)
        {
            var playerAliveMask = BuildAliveMask(session.PlayerCards);
            var enemyAliveMask = BuildAliveMask(session.EnemyCards);
            var result = BattleRules.EvaluateResult(playerAliveMask, enemyAliveMask);
            if (result != EBattleResult.InProgress)
            {
                session.Result.SetValue(result);
                return;
            }

            var actingSide = session.CurrentSide.Value;
            var attackerCards = session.GetCards(actingSide);
            var targetCards = session.GetCards(BattleRules.GetOppositeSide(actingSide));
            var attackerMask = actingSide == EBattleSide.Player ? playerAliveMask : enemyAliveMask;
            var targetMask = actingSide == EBattleSide.Player ? enemyAliveMask : playerAliveMask;
            var attackerSlot = BattleRules.FindNextLivingSlot(
                session.GetAttackCursor(actingSide),
                attackerMask);
            if (attackerSlot < 0)
            {
                session.Result.SetValue(BattleRules.EvaluateResult(playerAliveMask, enemyAliveMask));
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

            session.SetAttackCursor(actingSide, BattleRules.GetNextCursor(attackerSlot));
            session.CurrentAttacker.SetValue(attackerEntity);
            session.CurrentTarget.SetValue(targetEntity);

            var damage = BattleRules.ResolveKeywordDamage(attacker.Attack, target.Attack, attacker.Keywords);
            var blastDistance = BattleKeywordRules.Has(attacker.Keywords, EBattleKeyword.Blast)
                ? BattleKeywordRules.GetConfig(EBattleKeyword.Blast).BlastDistance
                : 0;
            var adjacentMask = damage.BlastDamage > 0
                ? BattleRules.GetAdjacentLivingMask(targetSlot, targetMask, blastDistance)
                : 0u;
            var attackerHealthBefore = attacker.CurrentHealth.Value;
            var targetHealthBefore = target.CurrentHealth.Value;

            attacker.SetCurrentHealthWithoutAliveCommit(attackerHealthBefore - damage.CounterDamage);
            target.SetCurrentHealthWithoutAliveCommit(targetHealthBefore - damage.MainDamage);
            for (var slot = 0; slot < targetCards.Length; slot++)
            {
                if ((adjacentMask & (1u << slot)) == 0)
                    continue;
                var adjacent = targetCards[slot].GetRawComponent<BattleCardRawComponent>();
                if (adjacent == null)
                    continue;
                var adjacentHealthBefore = adjacent.CurrentHealth.Value;
                adjacent.SetCurrentHealthWithoutAliveCommit(adjacentHealthBefore - damage.BlastDamage);
                DebugApi.Log(
                    $"[BattleKeyword] AdjacentDamage Action={session.ActionIndex + 1} " +
                    $"Slot={slot} Card={adjacent.CardNumber} Damage={damage.BlastDamage} " +
                    $"Health={adjacentHealthBefore}->{adjacent.CurrentHealth.Value}");
            }

            attacker.CommitAliveState();
            target.CommitAliveState();
            for (var slot = 0; slot < targetCards.Length; slot++)
            {
                if ((adjacentMask & (1u << slot)) == 0)
                    continue;
                targetCards[slot].GetRawComponent<BattleCardRawComponent>()?.CommitAliveState();
            }
            session.ActionIndex++;

            DebugApi.Log(
                $"[BattleKeyword] Action={session.ActionIndex} Side={actingSide} " +
                $"AttackerSlot={attackerSlot} AttackerCard={attacker.CardNumber} Keywords={attacker.Keywords} " +
                $"CandidateMask=0x{candidateMask:X} TargetSlot={targetSlot} TargetCard={target.CardNumber} " +
                $"MainDamage={damage.MainDamage} BlastDamage={damage.BlastDamage} " +
                $"CounterDamage={damage.CounterDamage} AdjacentMask=0x{adjacentMask:X} " +
                $"AttackerHealth={attackerHealthBefore}->{attacker.CurrentHealth.Value} " +
                $"TargetHealth={targetHealthBefore}->{target.CurrentHealth.Value}");

            var targetAliveMaskAfterCommit = BuildAliveMask(targetCards);
            var submittedTargetDeathMask = targetMask & ~targetAliveMaskAfterCommit;
            DebugApi.Log(
                $"[BattleKeyword] DeathCommit Action={session.ActionIndex} " +
                $"TargetDeathMask=0x{submittedTargetDeathMask:X} " +
                $"AttackerDied={(attacker.IsAlive.Value == false)}");

            playerAliveMask = BuildAliveMask(session.PlayerCards);
            enemyAliveMask = BuildAliveMask(session.EnemyCards);
            result = BattleRules.EvaluateResult(playerAliveMask, enemyAliveMask);
            session.Result.SetValue(result);
            DebugApi.Log(
                $"[BattleKeyword] Result Action={session.ActionIndex} " +
                $"PlayerAliveMask=0x{playerAliveMask:X} EnemyAliveMask=0x{enemyAliveMask:X} Result={result}");
            if (result == EBattleResult.InProgress)
                session.CurrentSide.SetValue(BattleRules.GetOppositeSide(actingSide));
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
