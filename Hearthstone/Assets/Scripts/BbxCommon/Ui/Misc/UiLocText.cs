using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BbxCommon.Ui
{
    /// <summary>
    /// Localization component for UI text. Attach to any GameObject that has a Text or TMP_Text component.
    /// In PreInit it auto-searches for Text / TMP_Text on the same GameObject.
    /// On UiOpen it applies the configured LocKey to the found text component.
    /// Call SetLocText(key, args) at any time to apply a different key with format arguments.
    /// </summary>
    public class UiLocText : MonoBehaviour, IUiPreInit, IUiOpen
    {
        [Tooltip("Localization key. Resolved via LocApi.GetLocText on UiOpen.")]
        public string LocKey;

        [HideInInspector]
        public TMP_Text TmpText;
        [HideInInspector]
        public Text LegacyText;

        bool IUiPreInit.OnUiPreInit(UiViewBase uiView)
        {
#if UNITY_EDITOR
            TmpText = GetComponent<TMP_Text>();
            if (TmpText == null)
                LegacyText = GetComponent<Text>();
#endif
            return true;
        }

        void IUiOpen.OnUiOpen(UiControllerBase uiController)
        {
            ApplyLocKey(LocKey);
        }

        /// <summary>
        /// Apply a localization key with optional string.Format arguments and display the result immediately.
        /// </summary>
        public void SetLocText(string key, params object[] args)
        {
            ApplyText(args != null && args.Length > 0 ? LocApi.GetLocText(key, args) : LocApi.GetLocText(key));
        }

        private void ApplyLocKey(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;
            ApplyText(LocApi.GetLocText(key));
        }

        private void ApplyText(string text)
        {
            if (TmpText != null)
                TmpText.text = text;
            else if (LegacyText != null)
                LegacyText.text = text;
        }
    }
}
