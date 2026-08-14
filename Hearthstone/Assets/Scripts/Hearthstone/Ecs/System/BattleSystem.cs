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

            var targetOrdinal = session.TargetRandom.NextInt(BattleRules.CountLiving(targetMask));
            var targetSlot = BattleRules.SelectLivingSlot(targetMask, targetOrdinal);
            var attackerEntity = attackerCards[attackerSlot];
            var targetEntity = targetCards[targetSlot];
            var attacker = attackerEntity.GetRawComponent<BattleCardRawComponent>();
            var target = targetEntity.GetRawComponent<BattleCardRawComponent>();

            session.SetAttackCursor(actingSide, BattleRules.GetNextCursor(attackerSlot));
            session.CurrentAttacker.SetValue(attackerEntity);
            session.CurrentTarget.SetValue(targetEntity);

            BattleRules.ResolveSimultaneousDamage(
                attacker.CurrentHealth.Value,
                attacker.Attack,
                target.CurrentHealth.Value,
                target.Attack,
                out var attackerHealth,
                out var targetHealth);
            attacker.SetCurrentHealth(attackerHealth);
            target.SetCurrentHealth(targetHealth);
            session.ActionIndex++;

            playerAliveMask = BuildAliveMask(session.PlayerCards);
            enemyAliveMask = BuildAliveMask(session.EnemyCards);
            result = BattleRules.EvaluateResult(playerAliveMask, enemyAliveMask);
            session.Result.SetValue(result);
            if (result == EBattleResult.InProgress)
                session.CurrentSide.SetValue(BattleRules.GetOppositeSide(actingSide));
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
    }
}
