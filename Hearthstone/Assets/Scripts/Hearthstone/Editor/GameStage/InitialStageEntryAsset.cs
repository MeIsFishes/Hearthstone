using BbxCommon.Editor;
using UnityEngine;

namespace Hearthstone.Editor
{
    /// <summary>
    /// 初始战斗 Stage Group 编辑器入口。奖励编号与永久数值在进入前已经分配完成。
    /// </summary>
    [CreateAssetMenu(
        fileName = "InitialStageEntry",
        menuName = "Hearthstone/GameStage Entry/Initial")]
    public sealed class InitialStageEntryAsset : GameStageEntryAsset
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
        private string m_BatchId = "initial-battle-reward-001";
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
                CreateStartupData();
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
            return TryEnterInitialStageGroup;
        }

        private bool TryEnterInitialStageGroup()
        {
            var engine = Object.FindObjectOfType<HearthstoneGameEngine>();
            if (engine == null)
                return false;
            engine.EnterBattleStageGroup(CreateStartupData());
            return true;
        }

        private BattleStageStartupData CreateStartupData()
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
            return new BattleStageStartupData(new PreparationRewardBatchStartupData(m_BatchId, grants));
        }
    }
}
