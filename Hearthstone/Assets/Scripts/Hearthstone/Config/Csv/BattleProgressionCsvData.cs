using System;
using BbxCommon;
using Random = Unity.Mathematics.Random;

namespace Hearthstone
{
    /// <summary>
    /// One preparation/battle round. Slot unlocks are incremental; draws are applied before this battle.
    /// </summary>
    public sealed class BattleProgressionCsvData : CsvDataBase<BattleProgressionCsvData>
    {
        public int BattleNumber { get; private set; }
        public int UnlockSlotCount { get; private set; }
        public int DrawCardCount { get; private set; }

        public override EDataLoad GetDataLoadType() => EDataLoad.Override;

        public override string[] GetTableNames() => new[] { nameof(BattleProgressionCsvData) };

        protected override void ReadLine()
        {
            BattleNumber = ParseIntFromKey(nameof(BattleNumber));
            UnlockSlotCount = ParseIntFromKey(nameof(UnlockSlotCount));
            DrawCardCount = ParseIntFromKey(nameof(DrawCardCount));
            ValidateValues();
            DataApi.SetData(BattleNumber, this);
        }

        public static BattleProgressionCsvData GetRequired(int battleNumber)
        {
            return DataApi.GetData<BattleProgressionCsvData>(battleNumber)
                ?? throw new InvalidOperationException($"Battle progression {battleNumber} is missing.");
        }

        public static int GetUnlockedSlotTotal(int battleNumber)
        {
            if (battleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleNumber));

            var total = 0;
            for (var round = 1; round <= battleNumber; round++)
            {
                var row = GetRequired(round);
                total = checked(total + row.UnlockSlotCount);
                if (total > RunCardRules.MaximumBattleSlotCount)
                {
                    throw new InvalidOperationException(
                        $"Battle progression {round} unlocks {total} slots, above the supported maximum " +
                        $"of {RunCardRules.MaximumBattleSlotCount}.");
                }
            }
            return total;
        }

        public static bool HasBattle(int battleNumber)
        {
            return battleNumber > 0 && DataApi.GetData<BattleProgressionCsvData>(battleNumber) != null;
        }

        private void ValidateValues()
        {
            if (BattleNumber <= 0)
                throw new InvalidOperationException("Battle progression number must be positive.");
            if (UnlockSlotCount < 0)
                throw new InvalidOperationException($"Battle progression {BattleNumber} slot unlock count cannot be negative.");
            if (DrawCardCount < 0)
                throw new InvalidOperationException($"Battle progression {BattleNumber} draw count cannot be negative.");
            if (BattleNumber == 1 &&
                (UnlockSlotCount != RunCardRules.InitialBattleSlotCount ||
                 DrawCardCount != RunCardRules.InitialDrawCardCount))
            {
                throw new InvalidOperationException(
                    $"Battle progression 1 must unlock {RunCardRules.InitialBattleSlotCount} slots and draw " +
                    $"{RunCardRules.InitialDrawCardCount} cards.");
            }
        }
    }

    /// <summary>
    /// One selectable enemy lineup for a battle. Multiple rows may share the same battle number.
    /// </summary>
    public sealed class EnemyLineupCsvData : CsvDataBase<EnemyLineupCsvData>
    {
        public int BattleNumber { get; private set; }
        public int[] CardNumbers { get; private set; } = Array.Empty<int>();

        public override EDataLoad GetDataLoadType() => EDataLoad.Override;

        public override string[] GetTableNames() => new[] { nameof(EnemyLineupCsvData) };

        protected override void ReadLine()
        {
            BattleNumber = ParseIntFromKey(nameof(BattleNumber));
            CardNumbers = ParseIntArrayFromKey(nameof(CardNumbers));
            ValidateValues();
            DataApi.SetAnonymousData(this);
        }

        public static EnemyLineupCsvData GetRandomRequired(int battleNumber, ref Random random)
        {
            if (battleNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleNumber));
            if (random.state == 0)
                throw new ArgumentException("Enemy-lineup random state cannot be zero.", nameof(random));

            EnemyLineupCsvData selected = null;
            var matchingCount = 0;
            foreach (var lineup in DataApi.GetEnumerator<EnemyLineupCsvData>())
            {
                if (lineup == null || lineup.BattleNumber != battleNumber)
                    continue;
                matchingCount++;
                if (random.NextInt(matchingCount) == 0)
                    selected = lineup;
            }

            return selected ?? throw new InvalidOperationException(
                $"Enemy lineup configuration for battle {battleNumber} is missing.");
        }

        private void ValidateValues()
        {
            if (BattleNumber <= 0)
                throw new InvalidOperationException("Enemy lineup battle number must be positive.");
            if (CardNumbers.Length == 0 || CardNumbers.Length > RunCardRules.MaximumBattleSlotCount)
            {
                throw new InvalidOperationException(
                    $"Enemy lineup {BattleNumber} must contain between 1 and " +
                    $"{RunCardRules.MaximumBattleSlotCount} cards.");
            }

            for (var index = 0; index < CardNumbers.Length; index++)
            {
                var cardNumber = CardNumbers[index];
                if (cardNumber < RunCardRules.FirstCardNumber ||
                    cardNumber > RunCardRules.LastCardNumber ||
                    cardNumber == RunCardRules.LockedCardNumber)
                {
                    throw new InvalidOperationException(
                        $"Enemy lineup {BattleNumber} references invalid card {cardNumber}.");
                }
            }
        }
    }
}
