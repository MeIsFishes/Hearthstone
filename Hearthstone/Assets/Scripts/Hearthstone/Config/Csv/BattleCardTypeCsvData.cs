using System;
using System.Collections.Generic;
using BbxCommon;
using Random = Unity.Mathematics.Random;

namespace Hearthstone
{
    public enum EBattleCardTier
    {
        Bronze,
        Silver,
        Gold,
        Legendary,
    }

    /// <summary>
    /// Defines the shared presentation and integer stat ranges for one battle card type.
    /// </summary>
    public sealed class BattleCardTypeCsvData : CsvDataBase<BattleCardTypeCsvData>
    {
        public int CardTypeId;
        public string DisplayName;
        public EBattleCardTier Tier;
        public int MinHealth;
        public int MaxHealth;
        public int MinAttack;
        public int MaxAttack;
        public EBattleKeyword InitialKeyword;
        public string AttackFrameAnimationKey;
        public string[] AttackAudioKeys = Array.Empty<string>();
        public float[] AttackAudioDelays = Array.Empty<float>();
        public float[] AttackAudioVolumes = Array.Empty<float>();
        public float[] HitDelays = Array.Empty<float>();

        public override EDataLoad GetDataLoadType()
        {
            return EDataLoad.Override;
        }

        public override string[] GetTableNames()
        {
            return new[] { nameof(BattleCardTypeCsvData) };
        }

        public int RollHealth(ref Random random)
        {
            return RollInclusive(MinHealth, MaxHealth, ref random);
        }

        public int RollAttack(ref Random random)
        {
            return RollInclusive(MinAttack, MaxAttack, ref random);
        }

        protected override void ReadLine()
        {
            CardTypeId = ParseIntFromKey(nameof(CardTypeId));
            DisplayName = GetStringFromKey(nameof(DisplayName));
            Tier = ReadTier();
            MinHealth = ParseIntFromKey(nameof(MinHealth));
            MaxHealth = ParseIntFromKey(nameof(MaxHealth));
            MinAttack = ParseIntFromKey(nameof(MinAttack));
            MaxAttack = ParseIntFromKey(nameof(MaxAttack));
            try
            {
                var rawKeyword = GetStringFromKey(nameof(InitialKeyword));
                if (string.IsNullOrWhiteSpace(rawKeyword) ||
                    string.Equals(rawKeyword, nameof(EBattleKeyword.None), StringComparison.OrdinalIgnoreCase))
                {
                    InitialKeyword = EBattleKeyword.None;
                }
                else if (Enum.TryParse(rawKeyword, true, out EBattleKeyword parsedKeyword))
                {
                    InitialKeyword = parsedKeyword;
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Battle card type {CardTypeId} has invalid initial keyword '{rawKeyword}'.");
                }
            }
            catch (KeyNotFoundException)
            {
                // Older test/custom tables remain valid and mean no keyword.
                InitialKeyword = EBattleKeyword.None;
            }
            ReadAttackPresentation();

            if (CardTypeId <= 0)
                throw new InvalidOperationException("Battle card type id must be positive.");
            if (string.IsNullOrWhiteSpace(DisplayName))
                throw new InvalidOperationException($"Battle card type {CardTypeId} has no display name.");
            var usesRuntimeFusionStats = CardTypeId >= RunCardRules.LockedCardNumber;
            if (usesRuntimeFusionStats)
            {
                if (MinHealth != 0 || MaxHealth != 0 || MinAttack != 0 || MaxAttack != 0)
                {
                    throw new InvalidOperationException(
                        $"Runtime-composed battle card type {CardTypeId} must leave its stat range empty.");
                }
                if (InitialKeyword != EBattleKeyword.None)
                {
                    throw new InvalidOperationException(
                        $"Runtime-composed battle card type {CardTypeId} must leave its initial keyword empty.");
                }
            }
            else
            {
                if (MinHealth <= 0 || MaxHealth < MinHealth)
                    throw new InvalidOperationException($"Battle card type {CardTypeId} has an invalid health range.");
                if (MinAttack < 0 || MaxAttack < MinAttack)
                    throw new InvalidOperationException($"Battle card type {CardTypeId} has an invalid attack range.");
            }
            var numericKeyword = (int)InitialKeyword;
            if (InitialKeyword != EBattleKeyword.None &&
                (BattleKeywordRules.Normalize(InitialKeyword) != InitialKeyword ||
                 (numericKeyword & (numericKeyword - 1)) != 0))
                throw new InvalidOperationException($"Battle card type {CardTypeId} must configure exactly one known initial keyword or None.");
            ValidateAttackPresentationLists();

            DataApi.SetData(CardTypeId, this);
        }

        private EBattleCardTier ReadTier()
        {
            try
            {
                var rawTier = GetStringFromKey(nameof(Tier));
                if (Enum.TryParse(rawTier, true, out EBattleCardTier parsedTier) &&
                    Enum.IsDefined(typeof(EBattleCardTier), parsedTier))
                    return parsedTier;
                throw new InvalidOperationException(
                    $"Battle card type {CardTypeId} has invalid tier '{rawTier}'.");
            }
            catch (KeyNotFoundException)
            {
                // Focused legacy tables without the tier column describe base cards.
                return EBattleCardTier.Bronze;
            }
        }

        private void ReadAttackPresentation()
        {
            try
            {
                AttackFrameAnimationKey = GetStringFromKey(nameof(AttackFrameAnimationKey));
                AttackAudioKeys = GetStringArrayFromKey(nameof(AttackAudioKeys));
                AttackAudioDelays = ParseFloatArrayFromKey(nameof(AttackAudioDelays));
                AttackAudioVolumes = ParseFloatArrayFromKey(nameof(AttackAudioVolumes));
                HitDelays = ParseFloatArrayFromKey(nameof(HitDelays));
            }
            catch (KeyNotFoundException)
            {
                // Older test/custom tables keep a safe presentation fallback.
                AttackFrameAnimationKey = string.Empty;
                AttackAudioKeys = Array.Empty<string>();
                AttackAudioDelays = Array.Empty<float>();
                AttackAudioVolumes = Array.Empty<float>();
                HitDelays = Array.Empty<float>();
            }
        }

        private void ValidateAttackPresentationLists()
        {
            if (AttackAudioKeys.Length != AttackAudioDelays.Length ||
                AttackAudioKeys.Length != AttackAudioVolumes.Length)
            {
                throw new InvalidOperationException(
                    $"Battle card type {CardTypeId} must configure the same number of attack audio keys, delays, and volumes.");
            }

            ValidateAscendingDelays(AttackAudioDelays, "attack audio");
            ValidateAscendingDelays(HitDelays, "hit");
            for (var i = 0; i < AttackAudioKeys.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(AttackAudioKeys[i]))
                    throw new InvalidOperationException($"Battle card type {CardTypeId} has an empty attack audio key at index {i}.");
                if (AttackAudioVolumes[i] < 0f || AttackAudioVolumes[i] > 1f)
                {
                    throw new InvalidOperationException(
                        $"Battle card type {CardTypeId} has an attack audio volume outside 0 through 1 at index {i}.");
                }
            }
        }

        private void ValidateAscendingDelays(float[] delays, string listName)
        {
            var previous = 0f;
            for (var i = 0; i < delays.Length; i++)
            {
                if (delays[i] < 0f)
                    throw new InvalidOperationException($"Battle card type {CardTypeId} has a negative {listName} delay at index {i}.");
                if (i > 0 && delays[i] < previous)
                    throw new InvalidOperationException($"Battle card type {CardTypeId} must sort its {listName} delays in ascending order.");
                previous = delays[i];
            }
        }

        private static int RollInclusive(int minimum, int maximum, ref Random random)
        {
            return minimum == maximum ? minimum : random.NextInt(minimum, maximum + 1);
        }
    }
}
