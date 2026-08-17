using System;
using UnityEngine;

namespace Hearthstone
{
    public static class NewPlayerGuideSave
    {
        public const string PreparationBasicsGuideId = "PreparationBasicsV1";
        private const string KeyPrefix = "Hearthstone.NewPlayerGuide.";

        private static readonly string[] KnownGuideIds =
        {
            PreparationBasicsGuideId,
        };

        public static bool HasTriggered(string guideId)
        {
            return PlayerPrefs.GetInt(GetKey(guideId), 0) == 1;
        }

        public static void MarkTriggered(string guideId)
        {
            PlayerPrefs.SetInt(GetKey(guideId), 1);
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            for (var index = 0; index < KnownGuideIds.Length; index++)
                PlayerPrefs.DeleteKey(GetKey(KnownGuideIds[index]));
            PlayerPrefs.Save();
        }

        internal static string GetKey(string guideId)
        {
            if (string.IsNullOrWhiteSpace(guideId))
                throw new ArgumentException("Guide id cannot be empty.", nameof(guideId));
            return KeyPrefix + guideId;
        }
    }
}
