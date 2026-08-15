using System;
using System.Text;

namespace Hearthstone
{
    [Flags]
    public enum EBattleKeyword
    {
        None = 0,
        Taunt = 1 << 0,
        LongShot = 1 << 1,
        Blast = 1 << 2,
        Charge = 1 << 3,
    }

    public static class BattleKeywordRules
    {
        public const EBattleKeyword AllKeywords =
            EBattleKeyword.Taunt |
            EBattleKeyword.LongShot |
            EBattleKeyword.Blast |
            EBattleKeyword.Charge;

        private static readonly EBattleKeyword[] DisplayOrder =
        {
            EBattleKeyword.Taunt,
            EBattleKeyword.LongShot,
            EBattleKeyword.Blast,
            EBattleKeyword.Charge,
        };

        public static EBattleKeyword Normalize(EBattleKeyword keywords)
        {
            return keywords & AllKeywords;
        }

        public static EBattleKeyword UnionKeywords(params EBattleKeyword[] keywordSets)
        {
            var result = EBattleKeyword.None;
            if (keywordSets == null)
                return result;
            for (var index = 0; index < keywordSets.Length; index++)
                result |= Normalize(keywordSets[index]);
            return Normalize(result);
        }

        public static bool Has(EBattleKeyword keywords, EBattleKeyword keyword)
        {
            return keyword != EBattleKeyword.None &&
                   (Normalize(keywords) & keyword) == keyword;
        }

        public static string FormatDisplayText(EBattleKeyword keywords)
        {
            keywords = Normalize(keywords);
            if (keywords == EBattleKeyword.None)
                return string.Empty;

            SortDisplayOrderFromConfiguration();
            var builder = new StringBuilder(24);
            var displayedCount = 0;
            for (var index = 0; index < DisplayOrder.Length; index++)
            {
                var keyword = DisplayOrder[index];
                if (Has(keywords, keyword) == false)
                    continue;
                if (displayedCount > 0)
                    builder.Append(displayedCount == 2 && Count(keywords) == 4 ? "\n" : " · ");
                builder.Append(GetDisplayName(keyword));
                displayedCount++;
            }
            return builder.ToString();
        }

        private static void SortDisplayOrderFromConfiguration()
        {
            for (var index = 1; index < DisplayOrder.Length; index++)
            {
                var keyword = DisplayOrder[index];
                var order = GetConfiguredDisplayOrder(keyword);
                var insertAt = index;
                while (insertAt > 0 && GetConfiguredDisplayOrder(DisplayOrder[insertAt - 1]) > order)
                {
                    DisplayOrder[insertAt] = DisplayOrder[insertAt - 1];
                    insertAt--;
                }
                DisplayOrder[insertAt] = keyword;
            }
        }

        private static int GetConfiguredDisplayOrder(EBattleKeyword keyword)
        {
            return GetConfig(keyword).DisplayOrder;
        }

        public static string GetDisplayName(EBattleKeyword keyword)
        {
            return GetConfig(keyword).DisplayName;
        }

        public static BattleKeywordCsvData GetConfig(EBattleKeyword keyword)
        {
            var numericKeyword = (int)keyword;
            if (keyword == EBattleKeyword.None || (numericKeyword & (numericKeyword - 1)) != 0)
                throw new ArgumentOutOfRangeException(nameof(keyword), keyword, "A single known keyword is required.");
            var data = BbxCommon.DataApi.GetData<BattleKeywordCsvData>((int)keyword);
            return data ?? throw new InvalidOperationException($"Battle keyword configuration '{keyword}' is missing.");
        }

        private static int Count(EBattleKeyword keywords)
        {
            var count = 0;
            for (var index = 0; index < DisplayOrder.Length; index++)
            {
                if (Has(keywords, DisplayOrder[index]))
                    count++;
            }
            return count;
        }
    }
}
