using System;
using System.Collections.Generic;
using System.IO;
using BbxCommon;
using UnityEngine;

namespace Hearthstone
{
    public static class CardCollectionCatalog
    {
        public static List<int> GetCollectibleCardNumbers()
        {
            var result = new List<int>();
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastCardNumber;
                 cardNumber++)
            {
                if (cardNumber == RunCardRules.LockedCardNumber)
                    continue;
                var card = DataApi.GetData<BattleCardCsvData>(cardNumber);
                if (card == null || card.FusionRecipeTypeIds.Count == 4)
                    continue;
                result.Add(cardNumber);
            }
            return result;
        }

        public static bool IsCollectible(int cardNumber)
        {
            if (cardNumber == RunCardRules.LockedCardNumber)
                return false;
            var card = DataApi.GetData<BattleCardCsvData>(cardNumber);
            return card != null && card.FusionRecipeTypeIds.Count != 4;
        }
    }

    public sealed class CardCollectionRepository
    {
        [Serializable]
        private sealed class SaveData
        {
            public int Version = 1;
            public int[] UnlockedCardNumbers = Array.Empty<int>();
        }

        private readonly string m_SavePath;
        private readonly HashSet<int> m_Unlocked = new HashSet<int>();
        private bool m_Loaded;

        public CardCollectionRepository(string savePath)
        {
            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentException("Card collection save path cannot be empty.", nameof(savePath));
            m_SavePath = Path.GetFullPath(savePath);
        }

        public string SavePath => m_SavePath;

        public bool IsUnlocked(int cardNumber)
        {
            EnsureLoaded();
            return m_Unlocked.Contains(cardNumber);
        }

        public HashSet<int> GetUnlockedSnapshot()
        {
            EnsureLoaded();
            return new HashSet<int>(m_Unlocked);
        }

        public bool Register(int cardNumber)
        {
            EnsureLoaded();
            if (CardCollectionCatalog.IsCollectible(cardNumber) == false || m_Unlocked.Add(cardNumber) == false)
                return false;
            Save();
            return true;
        }

        public bool RegisterMany(IEnumerable<int> cardNumbers)
        {
            if (cardNumbers == null)
                return false;
            EnsureLoaded();
            var changed = false;
            foreach (var cardNumber in cardNumbers)
            {
                if (CardCollectionCatalog.IsCollectible(cardNumber))
                    changed |= m_Unlocked.Add(cardNumber);
            }
            if (changed)
                Save();
            return changed;
        }

        public void Clear()
        {
            m_Unlocked.Clear();
            m_Loaded = true;
            DeleteIfPresent(m_SavePath);
            DeleteIfPresent(m_SavePath + ".tmp");
        }

        private void EnsureLoaded()
        {
            if (m_Loaded)
                return;
            m_Loaded = true;
            if (File.Exists(m_SavePath) == false)
                return;
            try
            {
                var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(m_SavePath));
                if (data?.UnlockedCardNumbers == null)
                    return;
                foreach (var cardNumber in data.UnlockedCardNumbers)
                {
                    if (CardCollectionCatalog.IsCollectible(cardNumber))
                        m_Unlocked.Add(cardNumber);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Card collection save could not be read and will be ignored: {exception.Message}");
            }
        }

        private void Save()
        {
            var directory = Path.GetDirectoryName(m_SavePath);
            if (string.IsNullOrEmpty(directory) == false)
                Directory.CreateDirectory(directory);
            var values = new int[m_Unlocked.Count];
            m_Unlocked.CopyTo(values);
            Array.Sort(values);
            var json = JsonUtility.ToJson(new SaveData { UnlockedCardNumbers = values }, true);
            var temporaryPath = m_SavePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(m_SavePath))
                File.Replace(temporaryPath, m_SavePath, null);
            else
                File.Move(temporaryPath, m_SavePath);
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    public static class CardCollectionSave
    {
        private const string SaveFileName = "card-collection.json";
        private static CardCollectionRepository s_Repository;

        public static string SavePath
        {
            get
            {
                var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var root = string.IsNullOrWhiteSpace(localApplicationData)
                    ? Application.persistentDataPath
                    : Path.Combine(localApplicationData, "DefaultCompany", "99升变");
                return Path.Combine(root, SaveFileName);
            }
        }

        public static CardCollectionRepository Repository =>
            s_Repository ?? (s_Repository = new CardCollectionRepository(SavePath));

        public static void RegisterRewardBatch(PreparationRewardBatchStartupData batch)
        {
            if (batch == null)
                return;
            var cardNumbers = new List<int>();
            for (var index = 0; index < batch.Grants.Count; index++)
                cardNumbers.Add(batch.Grants[index].CardNumber);
            Repository.RegisterMany(cardNumbers);
        }

        public static void RegisterOwnedCards(RunStateSingletonRawComponent runState)
        {
            if (runState == null)
                return;
            var cardNumbers = new List<int>();
            for (var cardNumber = RunCardRules.FirstCardNumber;
                 cardNumber <= RunCardRules.LastCardNumber;
                 cardNumber++)
            {
                if (runState.HasCard(cardNumber))
                    cardNumbers.Add(cardNumber);
            }
            Repository.RegisterMany(cardNumbers);
        }

        public static void Clear()
        {
            Repository.Clear();
        }
    }
}
