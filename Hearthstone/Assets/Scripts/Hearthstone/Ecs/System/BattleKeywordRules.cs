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
        public const int MaximumLevel = 4;
        public const EBattleKeyword AllKeywords =
            EBattleKeyword.Taunt |
            EBattleKeyword.LongShot |
            EBattleKeyword.Blast |
            EBattleKeyword.Charge;

        private const int BitsPerUpgradeLevel = 2;
        private const int UpgradeLevelMask = (1 << BitsPerUpgradeLevel) - 1;
        private const int FirstUpgradeLevelBit = 4;

        private static readonly EBattleKeyword[] KnownKeywords =
        {
            EBattleKeyword.Taunt,
            EBattleKeyword.LongShot,
            EBattleKeyword.Blast,
            EBattleKeyword.Charge,
        };

        private static readonly EBattleKeyword[] DisplayOrder =
        {
            EBattleKeyword.Taunt,
            EBattleKeyword.LongShot,
            EBattleKeyword.Blast,
            EBattleKeyword.Charge,
        };

        public static EBattleKeyword Normalize(EBattleKeyword keywords)
        {
            var normalized = EBattleKeyword.None;
            for (var index = 0; index < KnownKeywords.Length; index++)
            {
                var keyword = KnownKeywords[index];
                var level = GetStoredLevel(keywords, keyword, index);
                if (level > 0)
                    normalized = SetStoredLevel(normalized, keyword, index, level);
            }
            return normalized;
        }

        public static EBattleKeyword UnionKeywords(params EBattleKeyword[] keywordSets)
        {
            var result = EBattleKeyword.None;
            if (keywordSets == null)
                return result;
            for (var keywordIndex = 0; keywordIndex < KnownKeywords.Length; keywordIndex++)
            {
                var keyword = KnownKeywords[keywordIndex];
                var highestLevel = 0;
                for (var setIndex = 0; setIndex < keywordSets.Length; setIndex++)
                    highestLevel = Math.Max(highestLevel, GetLevel(keywordSets[setIndex], keyword));
                result = SetLevel(result, keyword, highestLevel);
            }
            return result;
        }

        public static EBattleKeyword MergeFusionKeywords(params EBattleKeyword[] keywordSets)
        {
            var result = EBattleKeyword.None;
            if (keywordSets == null)
                return result;
            for (var keywordIndex = 0; keywordIndex < KnownKeywords.Length; keywordIndex++)
            {
                var keyword = KnownKeywords[keywordIndex];
                var combinedLevel = 0;
                for (var setIndex = 0; setIndex < keywordSets.Length; setIndex++)
                    combinedLevel = Math.Min(MaximumLevel, combinedLevel + GetLevel(keywordSets[setIndex], keyword));
                result = SetLevel(result, keyword, combinedLevel);
            }
            return result;
        }

        public static bool Has(EBattleKeyword keywords, EBattleKeyword keyword)
        {
            if (keyword == EBattleKeyword.None)
                return false;
            var requested = Normalize(keyword);
            if (requested == EBattleKeyword.None)
                return false;
            for (var index = 0; index < KnownKeywords.Length; index++)
            {
                var knownKeyword = KnownKeywords[index];
                var requestedLevel = GetStoredLevel(requested, knownKeyword, index);
                if (requestedLevel > 0 && GetStoredLevel(keywords, knownKeyword, index) < requestedLevel)
                    return false;
            }
            return true;
        }

        public static int GetLevel(EBattleKeyword keywords, EBattleKeyword keyword)
        {
            var keywordIndex = GetKeywordIndex(keyword);
            return GetStoredLevel(keywords, keyword, keywordIndex);
        }

        public static EBattleKeyword SetLevel(
            EBattleKeyword keywords,
            EBattleKeyword keyword,
            int level)
        {
            if (level < 0 || level > MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Keyword level must be between 0 and {MaximumLevel}.");
            var keywordIndex = GetKeywordIndex(keyword);
            keywords = Normalize(keywords);
            var numericKeywords = (int)keywords;
            var levelShift = GetUpgradeLevelShift(keywordIndex);
            numericKeywords &= ~(int)keyword;
            numericKeywords &= ~(UpgradeLevelMask << levelShift);
            if (level == 0)
                return (EBattleKeyword)numericKeywords;
            return SetStoredLevel((EBattleKeyword)numericKeywords, keyword, keywordIndex, level);
        }

        public static string FormatDisplayText(EBattleKeyword keywords)
        {
            keywords = Normalize(keywords);
            if (keywords == EBattleKeyword.None)
                return string.Empty;

            SortDisplayOrderFromConfiguration();
            var builder = new StringBuilder(32);
            var displayedCount = 0;
            for (var index = 0; index < DisplayOrder.Length; index++)
            {
                var keyword = DisplayOrder[index];
                var level = GetLevel(keywords, keyword);
                if (level == 0)
                    continue;
                if (displayedCount > 0)
                    builder.Append('、');
                builder.Append(GetDisplayName(keyword));
                builder.Append(level);
                displayedCount++;
            }
            return builder.ToString();
        }

        public static string FormatDescriptionText(EBattleKeyword keywords)
        {
            keywords = Normalize(keywords);
            if (keywords == EBattleKeyword.None)
                return string.Empty;

            SortDisplayOrderFromConfiguration();
            var builder = new StringBuilder(320);
            var displayedCount = 0;
            for (var index = 0; index < DisplayOrder.Length; index++)
            {
                var keyword = DisplayOrder[index];
                var level = GetLevel(keywords, keyword);
                if (level == 0)
                    continue;
                if (displayedCount > 0)
                    builder.Append('\n');
                var config = GetConfig(keyword, level);
                builder.Append(config.DisplayName);
                builder.Append(level);
                builder.Append("：");
                builder.Append(config.Description);
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
            return GetConfig(keyword, 1);
        }

        public static BattleKeywordCsvData GetConfig(EBattleKeyword keyword, int level)
        {
            GetKeywordIndex(keyword);
            if (level < 1 || level > MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level), level, $"Keyword level must be between 1 and {MaximumLevel}.");
            var data = BbxCommon.DataApi.GetData<BattleKeywordCsvData>(GetConfigDataId(keyword, level));
            return data ?? throw new InvalidOperationException($"Battle keyword configuration '{keyword}' level {level} is missing.");
        }

        public static int GetConfigDataId(EBattleKeyword keyword, int level)
        {
            GetKeywordIndex(keyword);
            if (level < 1 || level > MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            return ((level - 1) << 4) | (int)keyword;
        }

        private static int GetKeywordIndex(EBattleKeyword keyword)
        {
            for (var index = 0; index < KnownKeywords.Length; index++)
            {
                if (KnownKeywords[index] == keyword)
                    return index;
            }
            throw new ArgumentOutOfRangeException(nameof(keyword), keyword, "A single known base keyword is required.");
        }

        private static int GetStoredLevel(
            EBattleKeyword keywords,
            EBattleKeyword keyword,
            int keywordIndex)
        {
            var numericKeywords = (int)keywords;
            if ((numericKeywords & (int)keyword) == 0)
                return 0;
            var storedUpgrade = (numericKeywords >> GetUpgradeLevelShift(keywordIndex)) & UpgradeLevelMask;
            return storedUpgrade + 1;
        }

        private static EBattleKeyword SetStoredLevel(
            EBattleKeyword keywords,
            EBattleKeyword keyword,
            int keywordIndex,
            int level)
        {
            var numericKeywords = (int)keywords | (int)keyword;
            numericKeywords |= (level - 1) << GetUpgradeLevelShift(keywordIndex);
            return (EBattleKeyword)numericKeywords;
        }

        private static int GetUpgradeLevelShift(int keywordIndex)
        {
            return FirstUpgradeLevelBit + keywordIndex * BitsPerUpgradeLevel;
        }
    }
}
