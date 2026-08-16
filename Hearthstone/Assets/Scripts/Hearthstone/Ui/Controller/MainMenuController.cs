using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public sealed class MainMenuController : UiControllerBase<MainMenuView>
    {
        private bool m_StartRequested;

        protected override void OnUiInit()
        {
            m_View.StartGameButton.onClick.AddListener(OnStartGameClicked);
            m_View.CollectionButton.onClick.AddListener(OnCollectionClicked);
            m_View.ClearDataButton.onClick.AddListener(OnClearDataClicked);
        }

        private void OnCollectionClicked()
        {
            var collection = UiApi.GetUiController<CardCollectionController>();
            if (collection == null)
            {
                DebugApi.LogError("Card collection UI is unavailable from the main menu scene.");
                return;
            }
            collection.ControllerWrapper.Show();
            ControllerWrapper.Hide();
        }

        private static void OnClearDataClicked()
        {
            CardCollectionSave.Clear();
            DebugApi.Log($"Card collection data cleared: {CardCollectionSave.SavePath}");
        }

        protected override void OnUiOpen()
        {
            m_StartRequested = false;
            m_View.StartGameButton.interactable = true;
            m_View.VersionLabel.text = $"v{Application.version}";
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
