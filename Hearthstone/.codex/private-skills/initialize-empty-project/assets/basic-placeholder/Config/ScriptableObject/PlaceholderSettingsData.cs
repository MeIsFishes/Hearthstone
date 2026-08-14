using BbxCommon;
using UnityEngine;

namespace __PROJECT_NAMESPACE__
{
    /// <summary>
    /// 空项目占位配置类型。Unity 不会自动创建对应资产，需在 Editor 中手动创建。
    /// </summary>
    [CreateAssetMenu(fileName = "PlaceholderSettingsData", menuName = "__PROJECT_NAME__/Placeholder Settings")]
    public sealed class PlaceholderSettingsData : BbxScriptableObject
    {
        public string ProjectDisplayName = "__PROJECT_NAME__";

        protected override void OnLoad()
        {
            DataApi.SetData(this);
        }
    }
}
