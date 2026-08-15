using System;

namespace Hearthstone
{
    public readonly struct BattleAttackDamageData
    {
        public int MainDamage { get; }
        public int BlastDamage { get; }
        public int CounterDamage { get; }

        public BattleAttackDamageData(int mainDamage, int blastDamage, int counterDamage)
        {
            MainDamage = mainDamage;
            BlastDamage = blastDamage;
            CounterDamage = counterDamage;
        }
    }

    /// <summary>
    /// 不依赖场景对象的核心战斗规则。
    /// </summary>
    public static class BattleRules
    {
        public const int CardsPerSide = RunCardRules.BattleSlotCount;
        public const int DefaultCardNumber = 1;
        public const float ActionInterval = 0.75f;
        public const float AttackLungeDuration = 0.36f;
        public const float AttackLungeDistance = 36f;
        public const int AttackEffectFrameCount = 8;
        public const int AttackEffectColumns = 4;
        public const int AttackEffectRows = 2;
        public const float AttackEffectFrameInterval = 0.06f;
        public const float HitFlashDuration = 0.16f;
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

        public static float GetAttackPresentationDuration(BattleCardTypeCsvData config)
        {
            var hitDelay = GetLatestDelay(config?.HitDelays);
            var audioDelay = GetLatestDelay(config?.AttackAudioDelays);
            var effectDuration = string.IsNullOrWhiteSpace(config?.AttackFrameAnimationKey)
                ? 0f
                : AttackEffectFrameCount * AttackEffectFrameInterval;
            return Math.Max(
                AttackLungeDuration,
                Math.Max(audioDelay, Math.Max(effectDuration, hitDelay + HitFlashDuration)));
        }

        private static float GetLatestDelay(float[] delays)
        {
            var latest = 0f;
            if (delays == null)
                return latest;
            for (var i = 0; i < delays.Length; i++)
                latest = Math.Max(latest, delays[i]);
            return latest;
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

        public static uint FilterTargetCandidateMask(uint aliveMask, uint tauntMask)
        {
            var livingTaunts = aliveMask & tauntMask;
            return livingTaunts == 0 ? aliveMask : livingTaunts;
        }

        public static int ScaleDamageFloor(int damage, int numerator, int denominator)
        {
            if (damage < 0)
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (numerator < 0)
                throw new ArgumentOutOfRangeException(nameof(numerator));
            if (denominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(denominator));
            return checked((int)((long)damage * numerator / denominator));
        }

        public static uint GetAdjacentLivingMask(int mainSlot, uint aliveMask, int distance)
        {
            if (mainSlot < 0 || mainSlot >= CardsPerSide)
                throw new ArgumentOutOfRangeException(nameof(mainSlot));
            if (distance < 0)
                throw new ArgumentOutOfRangeException(nameof(distance));
            var adjacentMask = 0u;
            for (var offset = 1; offset <= distance; offset++)
            {
                if (mainSlot - offset >= 0)
                    adjacentMask |= 1u << (mainSlot - offset);
                if (mainSlot + offset < CardsPerSide)
                    adjacentMask |= 1u << (mainSlot + offset);
            }
            return adjacentMask & aliveMask;
        }

        public static BattleAttackDamageData ResolveKeywordDamage(
            int attackerAttack,
            int targetAttack,
            EBattleKeyword attackerKeywords)
        {
            if (attackerAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(attackerAttack));
            if (targetAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(targetAttack));

            var isLongShot = BattleKeywordRules.Has(attackerKeywords, EBattleKeyword.LongShot);
            var mainDamage = attackerAttack;
            var counterDamage = targetAttack;
            if (isLongShot)
            {
                var longShot = BattleKeywordRules.GetConfig(EBattleKeyword.LongShot);
                mainDamage = ScaleDamageFloor(attackerAttack, longShot.DamageNumerator, longShot.DamageDenominator);
                if (longShot.SuppressCounterDamage)
                    counterDamage = 0;
            }
            var blastDamage = 0;
            if (BattleKeywordRules.Has(attackerKeywords, EBattleKeyword.Blast))
            {
                var blast = BattleKeywordRules.GetConfig(EBattleKeyword.Blast);
                blastDamage = ScaleDamageFloor(mainDamage, blast.DamageNumerator, blast.DamageDenominator);
            }
            return new BattleAttackDamageData(mainDamage, blastDamage, counterDamage);
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
