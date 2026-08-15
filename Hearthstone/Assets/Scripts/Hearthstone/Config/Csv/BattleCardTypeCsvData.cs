using System;
using System.Collections.Generic;
using BbxCommon;
using Random = Unity.Mathematics.Random;

namespace Hearthstone
{
    /// <summary>
    /// Defines the shared presentation and integer stat ranges for one battle card type.
    /// </summary>
    public sealed class BattleCardTypeCsvData : CsvDataBase<BattleCardTypeCsvData>
    {
        public int CardTypeId;
        public string DisplayName;
        public int MinHealth;
        public int MaxHealth;
        public int MinAttack;
        public int MaxAttack;
        public EBattleKeyword InitialKeyword;

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

            if (CardTypeId <= 0)
                throw new InvalidOperationException("Battle card type id must be positive.");
            if (string.IsNullOrWhiteSpace(DisplayName))
                throw new InvalidOperationException($"Battle card type {CardTypeId} has no display name.");
            if (MinHealth <= 0 || MaxHealth < MinHealth)
                throw new InvalidOperationException($"Battle card type {CardTypeId} has an invalid health range.");
            if (MinAttack < 0 || MaxAttack < MinAttack)
                throw new InvalidOperationException($"Battle card type {CardTypeId} has an invalid attack range.");
            var numericKeyword = (int)InitialKeyword;
            if (InitialKeyword != EBattleKeyword.None &&
                (BattleKeywordRules.Normalize(InitialKeyword) != InitialKeyword ||
                 (numericKeyword & (numericKeyword - 1)) != 0))
                throw new InvalidOperationException($"Battle card type {CardTypeId} must configure exactly one known initial keyword or None.");

            DataApi.SetData(CardTypeId, this);
        }

        private static int RollInclusive(int minimum, int maximum, ref Random random)
        {
            return minimum == maximum ? minimum : random.NextInt(minimum, maximum + 1);
        }
    }
}
