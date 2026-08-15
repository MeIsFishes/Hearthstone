using System;
using System.Collections.Generic;
using BbxCommon;

namespace Hearthstone
{
    public sealed class BattleCardCsvData : CsvDataBase<BattleCardCsvData>
    {
        private const int FusionRecipeKeyRadix = 16;

        public int CardNumber;
        public int CardTypeId;
        public string ArtworkKey;
        public List<int> FusionRecipeTypeIds = new List<int>();

        public bool IsFusionResult => FusionRecipeTypeIds.Count > 0;

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
            try
            {
                FusionRecipeTypeIds = new List<int>(ParseIntArrayFromKey(nameof(FusionRecipeTypeIds)));
            }
            catch (KeyNotFoundException)
            {
                // Legacy and focused test tables contain ordinary cards only.
                FusionRecipeTypeIds = new List<int>();
            }

            if (CardNumber < RunCardRules.FirstCardNumber || CardNumber > RunCardRules.LastCardNumber)
                throw new InvalidOperationException(
                    $"Battle card number {CardNumber} is outside the supported {RunCardRules.FirstCardNumber}~{RunCardRules.LastCardNumber} range.");
            if (CardTypeId <= 0)
                throw new InvalidOperationException($"Battle card {CardNumber} has an invalid card type id.");
            if (string.IsNullOrWhiteSpace(ArtworkKey))
                throw new InvalidOperationException($"Battle card {CardNumber} has no artwork key.");

            ValidateCardRoleAndFusionRecipe();

            DataApi.SetData(CardNumber, this);
            if (IsFusionResult == false)
                return;

            var recipeKey = CreateFusionRecipeDataKey(FusionRecipeTypeIds);
            var existing = DataApi.GetData<BattleCardCsvData>(recipeKey);
            if (existing != null && existing.CardNumber != CardNumber)
            {
                throw new InvalidOperationException(
                    $"Battle cards {existing.CardNumber} and {CardNumber} use the same fusion recipe.");
            }
            DataApi.SetData(recipeKey, this);
        }

        public static BattleCardCsvData GetFusionResult(
            int firstTypeId,
            int secondTypeId,
            int thirdTypeId,
            int fourthTypeId,
            int materialCount)
        {
            if (materialCount < RunCardRules.FusionMinimumRecipeMaterialCount ||
                materialCount > RunCardRules.FusionMaximumRecipeMaterialCount)
                return null;

            SortAscending(ref firstTypeId, ref secondTypeId);
            if (materialCount >= 3)
            {
                SortAscending(ref secondTypeId, ref thirdTypeId);
                SortAscending(ref firstTypeId, ref secondTypeId);
            }
            if (materialCount == 4)
            {
                SortAscending(ref thirdTypeId, ref fourthTypeId);
                SortAscending(ref secondTypeId, ref thirdTypeId);
                SortAscending(ref firstTypeId, ref secondTypeId);
            }

            var recipeKey = materialCount;
            recipeKey = recipeKey * FusionRecipeKeyRadix + firstTypeId;
            recipeKey = recipeKey * FusionRecipeKeyRadix + secondTypeId;
            if (materialCount >= 3)
                recipeKey = recipeKey * FusionRecipeKeyRadix + thirdTypeId;
            if (materialCount == 4)
                recipeKey = recipeKey * FusionRecipeKeyRadix + fourthTypeId;
            return DataApi.GetData<BattleCardCsvData>(-recipeKey);
        }

        private void ValidateCardRoleAndFusionRecipe()
        {
            if (CardNumber <= RunCardRules.LastOrdinaryCardNumber)
            {
                if (CardTypeId < 1 || CardTypeId > RunCardRules.BaseCardTypeCount)
                    throw new InvalidOperationException($"Ordinary battle card {CardNumber} must use a base card type.");
                if (IsFusionResult)
                    throw new InvalidOperationException($"Ordinary battle card {CardNumber} cannot define a fusion recipe.");
                return;
            }

            if (CardNumber == RunCardRules.LockedCardNumber)
            {
                if (CardTypeId != RunCardRules.LockedCardNumber || IsFusionResult)
                    throw new InvalidOperationException("The locked card-pool divider must use type 99 and cannot define a fusion recipe.");
                return;
            }

            if (CardTypeId != CardNumber)
                throw new InvalidOperationException($"Fusion battle card {CardNumber} must use its own card type id.");
            if (FusionRecipeTypeIds.Count < RunCardRules.FusionMinimumRecipeMaterialCount ||
                FusionRecipeTypeIds.Count > RunCardRules.FusionMaximumRecipeMaterialCount)
            {
                throw new InvalidOperationException(
                    $"Fusion battle card {CardNumber} must define a two-card, three-card, or four-card recipe.");
            }

            var ogreCount = 0;
            for (var index = 0; index < FusionRecipeTypeIds.Count; index++)
            {
                var typeId = FusionRecipeTypeIds[index];
                if (typeId < 1 || typeId > RunCardRules.BaseCardTypeCount)
                    throw new InvalidOperationException($"Fusion battle card {CardNumber} references non-base card type {typeId}.");
                if (index > 0 && FusionRecipeTypeIds[index - 1] > typeId)
                    throw new InvalidOperationException($"Fusion battle card {CardNumber} recipe must be sorted by card type id.");
                if (typeId == RunCardRules.OgreCardTypeId)
                    ogreCount++;
            }
            if (ogreCount > RunCardRules.MaximumOgreRecipeCount)
                throw new InvalidOperationException($"Fusion battle card {CardNumber} uses more than two ogres.");
        }

        private static int CreateFusionRecipeDataKey(IReadOnlyList<int> typeIds)
        {
            var recipeKey = typeIds.Count;
            for (var index = 0; index < typeIds.Count; index++)
                recipeKey = recipeKey * FusionRecipeKeyRadix + typeIds[index];
            return -recipeKey;
        }

        private static void SortAscending(ref int left, ref int right)
        {
            if (left <= right)
                return;
            var temporary = left;
            left = right;
            right = temporary;
        }
    }
}
