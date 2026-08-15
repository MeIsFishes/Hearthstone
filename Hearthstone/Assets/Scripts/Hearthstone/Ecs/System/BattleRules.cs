using System;

namespace Hearthstone
{
    /// <summary>
    /// 不依赖场景对象的核心战斗规则。
    /// </summary>
    public static class BattleRules
    {
        public const int CardsPerSide = RunCardRules.BattleSlotCount;
        public const int DefaultCardNumber = 1;
        public const float ActionInterval = 0.75f;
        public const string CardEntityGroup = "BattleCard";

        private static readonly int[] PlayerCardNumbers = { 4, 1, 40 };
        private static readonly int[] EnemyCardNumbers = { 5, 2, 9 };

        public static uint InitialAliveMask => (1u << CardsPerSide) - 1u;

        public static uint NormalizeSeed(uint seed)
        {
            return seed == 0 ? 1u : seed;
        }

        public static bool CanAct(EBattleResult result)
        {
            return result == EBattleResult.InProgress;
        }

        public static EBattleSide GetOppositeSide(EBattleSide side)
        {
            return side == EBattleSide.Player ? EBattleSide.Enemy : EBattleSide.Player;
        }

        public static int GetCardNumber(EBattleSide side, int slot)
        {
            if (slot < 0 || slot >= CardsPerSide)
                throw new ArgumentOutOfRangeException(nameof(slot));

            if (side == EBattleSide.Player)
                return PlayerCardNumbers[slot];
            if (side == EBattleSide.Enemy)
                return EnemyCardNumbers[slot];
            throw new ArgumentOutOfRangeException(nameof(side));
        }

        public static int FindNextLivingSlot(int startCursor, uint aliveMask)
        {
            if (aliveMask == 0)
                return -1;

            var normalizedCursor = NormalizeCursor(startCursor);
            for (var offset = 0; offset < CardsPerSide; offset++)
            {
                var slot = (normalizedCursor + offset) % CardsPerSide;
                if ((aliveMask & (1u << slot)) != 0)
                    return slot;
            }

            return -1;
        }

        public static int GetNextCursor(int actedSlot)
        {
            if (actedSlot < 0 || actedSlot >= CardsPerSide)
                throw new ArgumentOutOfRangeException(nameof(actedSlot));
            return (actedSlot + 1) % CardsPerSide;
        }

        public static int CountLiving(uint aliveMask)
        {
            var count = 0;
            for (var slot = 0; slot < CardsPerSide; slot++)
            {
                if ((aliveMask & (1u << slot)) != 0)
                    count++;
            }
            return count;
        }

        public static int SelectLivingSlot(uint aliveMask, int livingOrdinal)
        {
            var livingCount = CountLiving(aliveMask);
            if (livingOrdinal < 0 || livingOrdinal >= livingCount)
                throw new ArgumentOutOfRangeException(nameof(livingOrdinal));

            for (var slot = 0; slot < CardsPerSide; slot++)
            {
                if ((aliveMask & (1u << slot)) == 0)
                    continue;
                if (livingOrdinal == 0)
                    return slot;
                livingOrdinal--;
            }

            throw new InvalidOperationException("Unable to resolve a living card slot.");
        }

        public static void ResolveSimultaneousDamage(
            int attackerHealth,
            int attackerAttack,
            int targetHealth,
            int targetAttack,
            out int resolvedAttackerHealth,
            out int resolvedTargetHealth)
        {
            resolvedAttackerHealth = Math.Max(0, attackerHealth - targetAttack);
            resolvedTargetHealth = Math.Max(0, targetHealth - attackerAttack);
        }

        public static EBattleResult EvaluateResult(uint playerAliveMask, uint enemyAliveMask)
        {
            if (enemyAliveMask == 0)
                return EBattleResult.PlayerVictory;
            if (playerAliveMask == 0)
                return EBattleResult.EnemyVictory;
            return EBattleResult.InProgress;
        }

        private static int NormalizeCursor(int cursor)
        {
            var normalized = cursor % CardsPerSide;
            return normalized < 0 ? normalized + CardsPerSide : normalized;
        }
    }
}
