using System;
using System.Collections.Generic;
using BbxCommon;
using Random = Unity.Mathematics.Random;

namespace Hearthstone
{
    public static class BattleCardSimulationFactory
    {
        public static RunCardInstanceData Create(int cardNumber, ref Random random)
        {
            if (random.state == 0)
                throw new ArgumentException("Battle-card simulation random state cannot be zero.", nameof(random));
            var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber)
                ?? throw new InvalidOperationException($"Battle card simulation configuration {cardNumber} is missing.");
            if (cardNumber == RunCardRules.LockedCardNumber)
                throw new InvalidOperationException("The locked card-pool divider cannot be simulated.");

            if (cardConfig.IsFusionResult == false)
            {
                var typeConfig = DataApi.GetData<BattleCardTypeCsvData>(cardConfig.CardTypeId)
                    ?? throw new InvalidOperationException($"Battle card simulation type {cardConfig.CardTypeId} is missing.");
                return new RunCardInstanceData(
                    cardNumber,
                    typeConfig.RollAttack(ref random),
                    typeConfig.RollHealth(ref random));
            }

            var materials = new RunCardInstanceData[cardConfig.FusionRecipeTypeIds.Count];
            for (var index = 0; index < materials.Length; index++)
            {
                var materialCardNumber = SelectMaterialCardNumber(
                    cardConfig.FusionRecipeTypeIds[index],
                    materials,
                    index,
                    ref random);
                var materialConfig = DataApi.GetData<BattleCardCsvData>(materialCardNumber);
                var materialType = DataApi.GetData<BattleCardTypeCsvData>(materialConfig.CardTypeId)
                    ?? throw new InvalidOperationException(
                        $"Fusion simulation material type {materialConfig.CardTypeId} is missing.");
                materials[index] = new RunCardInstanceData(
                    materialCardNumber,
                    materialType.RollAttack(ref random),
                    materialType.RollHealth(ref random));
            }

            if (RunCardRules.TryCreateFusionResultInstance(cardConfig, materials, out var result) == false)
                throw new InvalidOperationException($"Fusion simulation card {cardNumber} exceeded the supported stat range.");
            return result;
        }

        public static RunCardInstanceData CreateDeterministic(int cardNumber)
        {
            var seed = unchecked((uint)cardNumber * 2654435761u) ^ 0xA341316Cu;
            if (seed == 0)
                seed = 1;
            var random = new Random(seed);
            return Create(cardNumber, ref random);
        }

        private static int SelectMaterialCardNumber(
            int cardTypeId,
            IReadOnlyList<RunCardInstanceData> selectedMaterials,
            int selectedCount,
            ref Random random)
        {
            var selectedCardNumber = 0;
            var candidateCount = 0;
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastOrdinaryCardNumber;
                 cardNumber++)
            {
                var cardConfig = DataApi.GetData<BattleCardCsvData>(cardNumber)
                    ?? throw new InvalidOperationException(
                        $"Fusion simulation material configuration {cardNumber} is missing.");
                if (cardConfig.CardTypeId != cardTypeId)
                    continue;
                var alreadySelected = false;
                for (var index = 0; index < selectedCount; index++)
                {
                    if (selectedMaterials[index].CardNumber != cardNumber)
                        continue;
                    alreadySelected = true;
                    break;
                }
                if (alreadySelected)
                    continue;

                candidateCount++;
                if (random.NextInt(candidateCount) == 0)
                    selectedCardNumber = cardNumber;
            }

            return selectedCardNumber != 0
                ? selectedCardNumber
                : throw new InvalidOperationException(
                    $"No distinct ordinary card is available for fusion simulation material type {cardTypeId}.");
        }
    }
}
