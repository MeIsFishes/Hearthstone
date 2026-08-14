using BbxCommon;
using BbxCommon.Ui;

namespace __PROJECT_NAMESPACE__
{
    /// <summary>
    /// 空项目占位 Controller。它监听占位 ECS Component，不保存第二份状态。
    /// </summary>
    public sealed class PlaceholderController : UiControllerBase<PlaceholderView>
    {
        protected override void InitListeners()
        {
            var state = EcsApi.GetSingletonRawComponent<PlaceholderStateSingletonRawComponent>();
            if (state == null)
                return;

            var listener = ModelWrapper.CreateVariableDirtyListener<bool>(
                EControllerLifeCycle.Show,
                RefreshStatus);
            listener.RebindTarget(state.Initialized);
        }

        protected override void OnUiShow()
        {
            var state = EcsApi.GetSingletonRawComponent<PlaceholderStateSingletonRawComponent>();
            RefreshStatus(state != null && state.Initialized.Value);
        }

        private void RefreshStatus(bool initialized)
        {
            if (m_View.StatusText != null)
                m_View.StatusText.text = initialized ? "Initialized" : "Initializing";
        }
    }
}
