using UnityEngine;
using Sirenix.OdinInspector;

namespace BbxCommon.Ui
{
    public abstract class HudViewBase : UiViewBase
    {
        [SerializeField]
        [FoldoutGroup("HUD")]
        internal bool AutoUpdatePos;
        [SerializeField]
        [FoldoutGroup("HUD"), ShowIf("AutoUpdatePos"), Tooltip("The offset relative to the entity it is bound with.")]
        internal Vector3 HudOffset;

        public bool IsAutoUpdatePos()
        {
            return AutoUpdatePos;
        }

        public Vector3 GetHudOffset()
        {
            return HudOffset;
        }

        public void SetHudOffset(Vector3 offset)
        {
            HudOffset = offset;
        }
    }
}
