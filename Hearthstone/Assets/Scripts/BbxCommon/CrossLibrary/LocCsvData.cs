using BbxCommon.Internal;

namespace BbxCommon
{
    /// <summary>
    /// Language list CSV. One row per language: Id, Name, CsvName (key for that language's translation CSV).
    /// Table name: LocLanguageList. Use EDataLoad.Addition so mods can add languages.
    /// </summary>
    public class LocLanguageCsvData : CsvDataBase<LocLanguageCsvData>
    {
        public string Id;
        public string Name;
        public string CsvName;

        public override string[] GetTableNames()
        {
            return new[] { "LocLanguageList" };
        }

        public override EDataLoad GetDataLoadType()
        {
            return EDataLoad.Addition;
        }

        protected override void ReadLine()
        {
            Id = GetStringFromKey("Id");
            Name = GetStringFromKey("Name");
            CsvName = GetStringFromKey("CsvName");
            LocApi.RegisterLanguage(Id, Name, CsvName);
        }
    }

    /// <summary>
    /// Translation table CSV per language. Columns: Key, Text. Do not use GetTableNames for auto-load; loaded when LocApi.SetCurrentLanguage runs LoadCsvByNameFunction(CsvName).
    /// Use EDataLoad.Addition so multiple CSVs for the same language (e.g. base + mod) merge.
    /// </summary>
    public class LocKeyCsvData : CsvDataBase<LocKeyCsvData>
    {
        public override string GetDataGroup()
        {
            return null;
        }

        public override string[] GetTableNames()
        {
            return System.Array.Empty<string>();
        }

        public override EDataLoad GetDataLoadType()
        {
            return EDataLoad.Addition;
        }

        protected override void ReadLine()
        {
            var key = GetStringFromKey("Key");
            var text = GetStringFromKey("Text");
            LocApi.AddTranslationForCurrentLoad(key, text);
        }
    }
}
