using System;
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

            if (CardTypeId <= 0)
                throw new InvalidOperationException("Battle card type id must be positive.");
            if (string.IsNullOrWhiteSpace(DisplayName))
                throw new InvalidOperationException($"Battle card type {CardTypeId} has no display name.");
            if (MinHealth <= 0 || MaxHealth < MinHealth)
                throw new InvalidOperationException($"Battle card type {CardTypeId} has an invalid health range.");
            if (MinAttack < 0 || MaxAttack < MinAttack)
                throw new InvalidOperationException($"Battle card type {CardTypeId} has an invalid attack range.");

            DataApi.SetData(CardTypeId, this);
        }

        private static int RollInclusive(int minimum, int maximum, ref Random random)
        {
            return minimum == maximum ? minimum : random.NextInt(minimum, maximum + 1);
        }
    }
}
