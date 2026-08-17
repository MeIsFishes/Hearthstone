using BbxCommon;
using BbxCommon.Ui;
using UnityEngine;

namespace Hearthstone
{
    public sealed class MainMenuController : UiControllerBase<MainMenuView>
    {
        private const int GoblinFrameCount = 12;
        private const float GoblinFrameDuration = 0.3f;

        private enum EGoblinAnimationPhase
        {
            Forward,
            Backward,
        }

        private bool m_StartRequested;
        private bool m_GoblinAnimationActive;
        private EGoblinAnimationPhase m_GoblinAnimationPhase;
        private int m_GoblinFrameIndex;
        private float m_GoblinAnimationElapsed;
        private bool m_GoblinBasePositionsCached;
        private Vector2 m_LeftGoblinBasePosition;
        private Vector2 m_RightGoblinBasePosition;

        protected override void OnUiInit()
        {
            m_View.StartGameButton.onClick.AddListener(OnStartGameClicked);
            m_View.CollectionButton.onClick.AddListener(OnCollectionClicked);
            m_View.ExitGameButton.onClick.AddListener(OnExitGameClicked);
            m_View.ClearDataButton.onClick.AddListener(OnClearDataClicked);
        }

        private static void OnExitGameClicked()
        {
            Application.Quit();
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
            NewPlayerGuideSave.Clear();
            DebugApi.Log($"Player data cleared: {CardCollectionSave.SavePath}");
        }

        protected override void OnUiOpen()
        {
            m_StartRequested = false;
            m_View.StartGameButton.interactable = true;
            m_View.VersionLabel.text = $"v{Application.version}";
        }

        protected override void OnUiShow()
        {
            CacheGoblinBasePositions();
            m_GoblinAnimationActive = HasValidGoblinAnimation();
            ResetGoblinAnimation();
            if (!m_GoblinAnimationActive)
                DebugApi.LogError($"Main menu goblin animation requires exactly {GoblinFrameCount} frames per character.");
        }

        protected override void OnUiHide()
        {
            m_GoblinAnimationActive = false;
        }

        protected override void OnUiUpdate(float deltaTime)
        {
            if (!m_GoblinAnimationActive || deltaTime <= 0f)
                return;

            m_GoblinAnimationElapsed += deltaTime;
            while (m_GoblinAnimationElapsed >= GoblinFrameDuration)
            {
                m_GoblinAnimationElapsed -= GoblinFrameDuration;
                AdvanceGoblinAnimation();
            }
        }

        private bool HasValidGoblinAnimation()
        {
            return m_View.LeftGoblinImage != null &&
                   m_View.RightGoblinImage != null &&
                   m_View.LeftGoblinFrames != null &&
                   m_View.RightGoblinFrames != null &&
                   m_View.LeftGoblinFrameOffsets != null &&
                   m_View.RightGoblinFrameOffsets != null &&
                   m_View.LeftGoblinFrames.Length == GoblinFrameCount &&
                   m_View.RightGoblinFrames.Length == GoblinFrameCount &&
                   m_View.LeftGoblinFrameOffsets.Length == GoblinFrameCount &&
                   m_View.RightGoblinFrameOffsets.Length == GoblinFrameCount;
        }

        private void CacheGoblinBasePositions()
        {
            if (m_GoblinBasePositionsCached ||
                m_View.LeftGoblinImage == null ||
                m_View.RightGoblinImage == null)
                return;
            m_LeftGoblinBasePosition = m_View.LeftGoblinImage.rectTransform.anchoredPosition;
            m_RightGoblinBasePosition = m_View.RightGoblinImage.rectTransform.anchoredPosition;
            m_GoblinBasePositionsCached = true;
        }

        private void ResetGoblinAnimation()
        {
            m_GoblinAnimationPhase = EGoblinAnimationPhase.Forward;
            m_GoblinFrameIndex = 0;
            m_GoblinAnimationElapsed = 0f;
            ApplyGoblinFrame();
        }

        private void AdvanceGoblinAnimation()
        {
            switch (m_GoblinAnimationPhase)
            {
                case EGoblinAnimationPhase.Forward:
                    if (m_GoblinFrameIndex < GoblinFrameCount - 1)
                    {
                        ++m_GoblinFrameIndex;
                    }
                    else
                    {
                        m_GoblinAnimationPhase = EGoblinAnimationPhase.Backward;
                        --m_GoblinFrameIndex;
                    }
                    break;
                case EGoblinAnimationPhase.Backward:
                    if (m_GoblinFrameIndex > 0)
                    {
                        --m_GoblinFrameIndex;
                    }
                    else
                    {
                        m_GoblinAnimationPhase = EGoblinAnimationPhase.Forward;
                        ++m_GoblinFrameIndex;
                    }
                    break;
            }
            ApplyGoblinFrame();
        }

        private void ApplyGoblinFrame()
        {
            if (!m_GoblinAnimationActive)
                return;
            m_View.LeftGoblinImage.sprite = m_View.LeftGoblinFrames[m_GoblinFrameIndex];
            m_View.RightGoblinImage.sprite = m_View.RightGoblinFrames[m_GoblinFrameIndex];
            m_View.LeftGoblinImage.rectTransform.anchoredPosition =
                m_LeftGoblinBasePosition + m_View.LeftGoblinFrameOffsets[m_GoblinFrameIndex];
            m_View.RightGoblinImage.rectTransform.anchoredPosition =
                m_RightGoblinBasePosition + m_View.RightGoblinFrameOffsets[m_GoblinFrameIndex];
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
