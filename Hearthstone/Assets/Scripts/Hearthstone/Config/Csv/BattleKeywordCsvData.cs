using System;
using BbxCommon;

namespace Hearthstone
{
    public sealed class BattleKeywordCsvData : CsvDataBase<BattleKeywordCsvData>
    {
        public EBattleKeyword Keyword;
        public string DisplayName;
        public string Description;
        public int DisplayOrder;
        public int DamageNumerator;
        public int DamageDenominator;
        public int BlastDistance;
        public int AttackGain;
        public int HealthGain;
        public bool SuppressCounterDamage;

        public override EDataLoad GetDataLoadType() => EDataLoad.Override;

        public override string[] GetTableNames() => new[] { nameof(BattleKeywordCsvData) };

        protected override void ReadLine()
        {
            Keyword = ParseEnumFromKey<EBattleKeyword>(nameof(Keyword));
            DisplayName = GetStringFromKey(nameof(DisplayName));
            Description = GetStringFromKey(nameof(Description));
            DisplayOrder = ParseIntFromKey(nameof(DisplayOrder));
            DamageNumerator = ParseIntFromKey(nameof(DamageNumerator));
            DamageDenominator = ParseIntFromKey(nameof(DamageDenominator));
            BlastDistance = ParseIntFromKey(nameof(BlastDistance));
            AttackGain = ParseIntFromKey(nameof(AttackGain));
            HealthGain = ParseIntFromKey(nameof(HealthGain));
            SuppressCounterDamage = ParseBoolFromKey(nameof(SuppressCounterDamage));
            var numericKeyword = (int)Keyword;
            if (Keyword == EBattleKeyword.None ||
                BattleKeywordRules.Normalize(Keyword) != Keyword ||
                (numericKeyword & (numericKeyword - 1)) != 0)
                throw new InvalidOperationException($"Battle keyword '{Keyword}' must contain exactly one known flag.");
            if (string.IsNullOrWhiteSpace(DisplayName))
                throw new InvalidOperationException($"Battle keyword '{Keyword}' has no display name.");
            if (string.IsNullOrWhiteSpace(Description))
                throw new InvalidOperationException($"Battle keyword '{Keyword}' has no description.");
            if (DisplayOrder < 0)
                throw new InvalidOperationException($"Battle keyword '{Keyword}' has an invalid display order.");
            if (DamageNumerator < 0 || DamageDenominator <= 0)
                throw new InvalidOperationException($"Battle keyword '{Keyword}' has an invalid damage ratio.");
            if (BlastDistance < 0 || AttackGain < 0 || HealthGain < 0)
                throw new InvalidOperationException($"Battle keyword '{Keyword}' has an invalid non-negative behavior value.");
            DataApi.SetData((int)Keyword, this);
        }
    }
}
