using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Reflection;

namespace BbxCommon.Ui
{
    public abstract class UiViewBase : MonoBehaviour
    {
        private const string ButtonClickAudioKey = "click_001";
        private const float ButtonClickAudioVolume = 0.7f;

        #region Common
        public bool DefaultShow = true;

        [NonSerialized]
        public UiControllerBase UiController;

        [SerializeField]
        internal List<Component> BbxUiItems = new();
        internal List<Component> UiInits = new();
        internal List<Component> UiOpens = new();
        internal List<Component> UiShows = new();
        internal List<Component> UiUpdates = new();
        internal List<Component> UiHides = new();
        internal List<Component> UiCloses = new();
        internal List<Component> UiDestroys = new();
        private bool m_ButtonClickAudioInitialized;

#if UNITY_EDITOR
        [Button("Pre-UiInit")]
        private void PreUiInit()
        {
            UiApi.EditorOperation.PreInitializeView(this);
        }

        internal void EditorPreInitialize()
        {
            var uiPreInitRemoves = GetComponentsInChildren<IUiPreInitRemove>(true);
            foreach (var item in uiPreInitRemoves)
            {
                if (item.DontRemove == false)
                    DestroyImmediate((Component)item);
            }
            BbxUiItems.RemoveAll(item => item == null);

            int loop = 0;
            while (true)
            {
                bool end = true;
                var uiPreInits = GetComponentsInChildren<IUiPreInit>(true);
                foreach (var item in uiPreInits)
                {
                    if (item.OnUiPreInit(this) == false)
                        end = false;
                    EditorUtility.SetDirty((Component)item);
                }
                EditorUtility.SetDirty(this);

                if (end) break;

                loop++;
                if (loop > 8)
                {
                    DebugApi.LogError("UiPreInit has run too many loops! Is there something wrong?");
                    break;
                }
            }

            var uiInits = GetComponentsInChildren<IUiInit>(true);
            foreach (var item in uiInits)
            {
                if (BbxUiItems.Contains((Component)item) == false)
                    BbxUiItems.Add((Component)item);
            }

            var uiOpens = GetComponentsInChildren<IUiOpen>(true);
            foreach (var item in uiOpens)
            {
                if (BbxUiItems.Contains((Component)item) == false)
                    BbxUiItems.Add((Component)item);
            }

            var uiShows = GetComponentsInChildren<IUiShow>(true);
            foreach (var item in uiShows)
            {
                if (BbxUiItems.Contains((Component)item) == false)
                    BbxUiItems.Add((Component)item);
            }

            var uiUpdates = GetComponentsInChildren<IUiUpdate>(true);
            foreach (var item in uiUpdates)
            {
                if (BbxUiItems.Contains((Component)item) == false)
                    BbxUiItems.Add((Component)item);
            }

            var uiHides = GetComponentsInChildren<IUiHide>(true);
            foreach (var item in uiHides)
            {
                if (BbxUiItems.Contains((Component)item) == false)
                    BbxUiItems.Add((Component)item);
            }

            var uiCloses = GetComponentsInChildren<IUiClose>(true);
            foreach (var item in uiCloses)
            {
                if (BbxUiItems.Contains((Component)item) == false)
                    BbxUiItems.Add((Component)item);
            }

            var uiDestroys = GetComponentsInChildren<IUiDestroy>(true);
            foreach (var item in uiDestroys)
            {
                if (BbxUiItems.Contains((Component)item) == false)
                    BbxUiItems.Add((Component)item);
            }
        }

        [Button("Export as Pre-load")]
        private void ExportAsPreLoadUi()
        {
            UiApi.EditorOperation.ExportPreloadedView(this);
        }
#endif

        /// <summary>
        /// Cache all components by their calling timing.
        /// </summary>
        internal void InitBbxUiItem()
        {
            InitButtonClickAudio();
            foreach (var item in BbxUiItems)
            {
                if (item is IUiInit)
                    UiInits.Add(item);
                if (item is IUiOpen)
                    UiOpens.Add(item);
                if (item is IUiShow)
                    UiShows.Add(item);
                if (item is IUiUpdate)
                    UiUpdates.Add(item);
                if (item is IUiHide)
                    UiHides.Add(item);
                if (item is IUiClose)
                    UiCloses.Add(item);
                if (item is IUiDestroy)
                    UiDestroys.Add(item);
            }
        }

        private void InitButtonClickAudio()
        {
            if (m_ButtonClickAudioInitialized)
                return;

            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.GetComponentInParent<UiViewBase>(true) == this)
                    button.onClick.AddListener(PlayButtonClickAudio);
            }
            m_ButtonClickAudioInitialized = true;
        }

        private static void PlayButtonClickAudio()
        {
            AudioApi.Play(ButtonClickAudioKey, ButtonClickAudioVolume);
        }

        public abstract Type GetControllerType();
        #endregion

        #region ControllerTypeId
        // In generic inheritance, for example, think of the code below:
        // UiMyController : UiControllerBase<UiMyView>
        // UiMyController and UiControllerBase<UiMyView> is not the same class. In some cases, we operate a
        // controller with its base class reference, then we can get its type id by its view, instead of using
        // ClassTypeId<UiControllerBase, TController>, because its view holds its real type.

        private int m_ControllerTypeId;
        private bool m_ControllerTypeIdInited;

        internal int GetControllerTypeId()
        {
            if (m_ControllerTypeIdInited == false)
            {
                var type = typeof(ClassTypeId<,>).MakeGenericType(typeof(UiControllerBase), GetControllerType());
                var method = type.GetMethod("GetId", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                m_ControllerTypeId = (int)method.Invoke(null, null);
                m_ControllerTypeIdInited = true;
            }
            return m_ControllerTypeId;
        }
        #endregion
    }
}
