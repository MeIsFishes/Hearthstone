using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthstone
{
    public static class BattleCardItemUiBuilder
    {
        private const string PrefabPath = "Assets/Resources/Ui/BattleCardItem.prefab";
        private const string NarrowFramePath =
            "Assets/Resources/Art/BattleCards/UI/CardFrame-v3.png";

        public static void Build()
        {
            var narrowFrame = AssetDatabase.LoadAssetAtPath<Sprite>(NarrowFramePath);
            if (narrowFrame == null)
                throw new InvalidOperationException($"Narrow battle card frame is missing at '{NarrowFramePath}'.");

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var view = root.GetComponent<BattleCardItemView>();
                if (view == null)
                    throw new InvalidOperationException("BattleCardItemView is missing from the card prefab root.");

                ConfigureFrame(view.CardFrame, narrowFrame, "CardFrameOverlay");
                ConfigureFrame(view.AttackerHighlight, narrowFrame, "AttackerHighlight");
                ConfigureFrame(view.TargetHighlight, narrowFrame, "TargetHighlight");

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureFrame(Image image, Sprite sprite, string objectName)
        {
            if (image == null)
                throw new InvalidOperationException($"{objectName} image reference is missing.");

            var rectTransform = image.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;
        }
    }
}
