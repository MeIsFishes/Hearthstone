using BbxCommon;
using BbxCommon.Ui;

namespace Hearthstone
{
    public sealed class MainMenuController : UiControllerBase<MainMenuView>
    {
        private bool m_StartRequested;

        protected override void OnUiInit()
        {
            m_View.StartGameButton.onClick.AddListener(OnStartGameClicked);
        }

        protected override void OnUiOpen()
        {
            m_StartRequested = false;
            m_View.StartGameButton.interactable = true;
        }

        private void OnStartGameClicked()
        {
            if (m_StartRequested)
                return;
            if (HearthstoneGameEngine.Instance == null)
            {
                DebugApi.LogError("Main menu cannot start a run because HearthstoneGameEngine is unavailable.");
                return;
            }

            m_StartRequested = true;
            m_View.StartGameButton.interactable = false;
            HearthstoneGameEngine.Instance.StartNewRun();
        }
    }
}
