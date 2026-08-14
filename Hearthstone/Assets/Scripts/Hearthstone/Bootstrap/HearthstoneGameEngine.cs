using BbxCommon;

namespace Hearthstone
{
    /// <summary>
    /// 项目运行入口。当前默认进入核心自动战斗模式。
    /// </summary>
    public sealed class HearthstoneGameEngine : GameEngineBase<HearthstoneGameEngine>
    {
        private GameStage m_BattleStage;

        protected override void OnAwake()
        {
            RegisterSystemOrder(
                typeof(InputSystem),
                typeof(BattleSystem),
                typeof(TaskSystem));

            EnterBattleStageGroup();
        }

        /// <summary>
        /// 进入完整的核心战斗 StageGroup。
        /// </summary>
        public void EnterBattleStageGroup()
        {
            m_BattleStage ??= BattleStages.CreateBattleStage(this);
            StageWrapper.SetActiveGameStage(m_BattleStage);
        }
    }
}
