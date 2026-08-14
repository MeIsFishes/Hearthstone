using System;
using System.Collections.Generic;

namespace BbxCommon
{
    /// <summary>
    /// Language info for one entry in the language list CSV.
    /// </summary>
    public struct LocLanguageInfo
    {
        public string Id;
        public string Name;
        public string CsvName;
    }

    /// <summary>
    /// Localization API. Assign <see cref="LoadCsvByNameFunction"/> once at startup (e.g. from game engine), then use <see cref="SetCurrentLanguage"/>.
    /// </summary>
    public static class LocApi
    {
        /// <summary>
        /// Loads LocKeyCsvData by CsvName. Set before the first <see cref="SetCurrentLanguage"/> that should load tables.
        /// </summary>
        public static Action<string> LoadCsvByNameFunction;

        private static string m_CurrentLanguageId;
        private static readonly List<LocLanguageInfo> m_Languages = new();
        private static readonly Dictionary<string, Dictionary<string, string>> m_Translations = new();
        private static string m_CurrentLanguageForLoading;

        /// <summary>
        /// Get translated text for the given key in the current language. Returns the key itself if not found.
        /// </summary>
        public static string GetLocText(string key)
        {
            if (string.IsNullOrEmpty(m_CurrentLanguageId))
                return key;
            if (m_Translations.TryGetValue(m_CurrentLanguageId, out var dict) && dict.TryGetValue(key, out var text))
                return text;
            return key;
        }

        /// <summary>
        /// Get translated text and format it with the given arguments via string.Format.
        /// </summary>
        public static string GetLocText(string key, params object[] args)
        {
            return string.Format(GetLocText(key), args);
        }

        /// <summary>
        /// Get the current language id (may be null if not set).
        /// </summary>
        public static string GetCurrentLanguage()
        {
            return m_CurrentLanguageId;
        }

        /// <summary>
        /// Set the current language. Unloads the previous language's table so only the active language stays in memory.
        /// When <paramref name="languageId"/> is null or empty, clears all loaded tables and unsets the current language.
        /// Requires the id to exist in the language list (from LocLanguageList CSV) to load; otherwise only the current id is updated.
        /// </summary>
        public static void SetCurrentLanguage(string languageId)
        {
            if (string.IsNullOrEmpty(languageId))
            {
                m_Translations.Clear();
                m_CurrentLanguageId = null;
                m_CurrentLanguageForLoading = null;
                return;
            }

            var csvName = ResolveCsvName(languageId);
            if (LoadCsvByNameFunction == null || csvName == null)
            {
                if (!string.IsNullOrEmpty(m_CurrentLanguageId) && m_CurrentLanguageId != languageId)
                    m_Translations.Remove(m_CurrentLanguageId);
                m_CurrentLanguageId = languageId;
                return;
            }

            if (m_CurrentLanguageId == languageId && m_Translations.ContainsKey(languageId))
                return;

            if (!string.IsNullOrEmpty(m_CurrentLanguageId) && m_CurrentLanguageId != languageId)
                m_Translations.Remove(m_CurrentLanguageId);

            m_CurrentLanguageId = languageId;

            if (!m_Translations.TryGetValue(languageId, out var dict))
            {
                dict = new Dictionary<string, string>();
                m_Translations[languageId] = dict;
            }
            else
                dict.Clear();

            m_CurrentLanguageForLoading = languageId;
            LoadCsvByNameFunction(csvName);
        }

        /// <summary>
        /// Enumerate registered languages for UI (e.g. language selector). Do not modify the list.
        /// </summary>
        public static IReadOnlyList<LocLanguageInfo> GetLanguageList()
        {
            return m_Languages;
        }

        internal static void RegisterLanguage(string id, string name, string csvName)
        {
            m_Languages.Add(new LocLanguageInfo { Id = id, Name = name, CsvName = csvName });
            if (string.IsNullOrEmpty(m_CurrentLanguageId))
                m_CurrentLanguageId = id;
        }

        internal static void AddTranslationForCurrentLoad(string key, string text)
        {
            if (string.IsNullOrEmpty(m_CurrentLanguageForLoading))
                return;
            if (m_Translations.TryGetValue(m_CurrentLanguageForLoading, out var dict))
                dict[key] = text;
        }

        private static string ResolveCsvName(string languageId)
        {
            for (int i = 0; i < m_Languages.Count; i++)
            {
                if (m_Languages[i].Id == languageId)
                    return m_Languages[i].CsvName;
            }
            return null;
        }
    }
}
