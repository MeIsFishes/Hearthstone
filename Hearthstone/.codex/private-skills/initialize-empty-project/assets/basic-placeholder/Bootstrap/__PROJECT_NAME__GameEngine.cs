using BbxCommon;

namespace __PROJECT_NAMESPACE__
{
    /// <summary>
    /// 空项目占位入口。首个真实模式建立后，将 Stage 组合替换为项目自己的启动流程。
    /// </summary>
    public sealed class __PROJECT_NAME__GameEngine : GameEngineBase<__PROJECT_NAME__GameEngine>
    {
        private GameStage m_BaseStage;

        protected override void OnAwake()
        {
            RegisterSystemOrder(
                typeof(InputSystem),
                typeof(PlaceholderStateSystem),
                typeof(TaskSystem));

            EnterInitialStageGroup();
        }

        /// <summary>
        /// 首次初始化必须保留的 GameStage Group 入口。真实模式建立后，以同样方式新增具名入口。
        /// </summary>
        public void EnterInitialStageGroup()
        {
            m_BaseStage ??= PlaceholderStages.CreateBaseStage(this);
            StageWrapper.SetActiveGameStage(m_BaseStage);
        }
    }
}
