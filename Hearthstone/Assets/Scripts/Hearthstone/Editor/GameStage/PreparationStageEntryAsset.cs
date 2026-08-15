using BbxCommon.Editor;
using UnityEngine;

namespace Hearthstone.Editor
{
    [CreateAssetMenu(
        fileName = "PreparationStageEntry",
        menuName = "Hearthstone/GameStage Entry/Preparation")]
    public sealed class PreparationStageEntryAsset : GameStageEntryAsset
    {
        [System.Serializable]
        private struct RewardGrantEntry
        {
            public int CardNumber;
            public int Attack;
            public int MaxHealth;

            public RewardGrantEntry(int cardNumber, int attack, int maxHealth)
            {
                CardNumber = cardNumber;
                Attack = attack;
                MaxHealth = maxHealth;
            }
        }

        [SerializeField]
        private string m_BatchId = "preparation-isolated-001";
        [SerializeField]
        private RewardGrantEntry[] m_RewardGrants =
        {
            new RewardGrantEntry(2, 5, 3),
            new RewardGrantEntry(3, 4, 4),
            new RewardGrantEntry(5, 3, 5),
            new RewardGrantEntry(6, 5, 4),
            new RewardGrantEntry(7, 6, 2),
        };

        public override bool ValidateEntry(out string error)
        {
            try
            {
                CreateBatch();
                error = string.Empty;
                return true;
            }
            catch (System.Exception exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public override System.Func<bool> CreateStageGroupBuildCallback()
        {
            return TryEnterPreparationStageGroup;
        }

        private bool TryEnterPreparationStageGroup()
        {
            var engine = Object.FindObjectOfType<HearthstoneGameEngine>();
            if (engine == null)
                return false;
            engine.EnterPreparationStageGroup(CreateBatch());
            return true;
        }

        private PreparationRewardBatchStartupData CreateBatch()
        {
            if (m_RewardGrants == null)
                throw new System.InvalidOperationException("Reward grants are missing.");
            var grants = new RewardCardGrantStartupData[m_RewardGrants.Length];
            for (var index = 0; index < grants.Length; index++)
            {
                var source = m_RewardGrants[index];
                grants[index] = new RewardCardGrantStartupData(
                    source.CardNumber,
                    source.Attack,
                    source.MaxHealth);
            }
            return new PreparationRewardBatchStartupData(m_BatchId, grants);
        }
    }
}
