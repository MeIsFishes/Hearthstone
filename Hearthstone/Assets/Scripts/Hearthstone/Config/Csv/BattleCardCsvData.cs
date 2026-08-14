using System;
using BbxCommon;

namespace Hearthstone
{
    public sealed class BattleCardCsvData : CsvDataBase<BattleCardCsvData>
    {
        public int CardNumber;
        public int CardTypeId;
        public string ArtworkKey;

        public override EDataLoad GetDataLoadType()
        {
            return EDataLoad.Override;
        }

        public override string[] GetTableNames()
        {
            return new[] { nameof(BattleCardCsvData) };
        }

        protected override void ReadLine()
        {
            CardNumber = ParseIntFromKey(nameof(CardNumber));
            CardTypeId = ParseIntFromKey(nameof(CardTypeId));
            ArtworkKey = GetStringFromKey(nameof(ArtworkKey));

            if (CardNumber < 1 || CardNumber > 98)
                throw new InvalidOperationException($"Battle card number {CardNumber} is outside the supported 1~98 range.");
            if (CardTypeId <= 0)
                throw new InvalidOperationException($"Battle card {CardNumber} has an invalid card type id.");
            if (string.IsNullOrWhiteSpace(ArtworkKey))
                throw new InvalidOperationException($"Battle card {CardNumber} has no artwork key.");

            DataApi.SetData(CardNumber, this);
        }
    }
}
