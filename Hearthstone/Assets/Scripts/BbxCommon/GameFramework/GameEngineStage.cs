using BbxCommon.Internal;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace BbxCommon
{
    public partial class GameEngineBase<TEngine>
    {
        private GameStage CreateGameEngineStage()
        {
            var stage = StageWrapper.CreateStage("Game Engine Stage");

            stage.AddDataGroup("GameEngineDefault");

            stage.AddLoadItem<InitReflectionAndResource>();
            // Localization depends on LocLanguageList from GameEngineDefault.
            // Keep it after DataGroup loading so the first registered language can
            // be resolved and loaded before any gameplay UiScene is opened.
            stage.AddLateLoadItem<LoadLocTranslations>();

            stage.AddUpdateSystem<InputSystem>();
            stage.AddUpdateSystem<TaskSystem>();

            return stage;
        }

        private class InitReflectionAndResource : IStageLoad
        {
            public void Load(GameStage stage)
            {
                // reflect types
                foreach (var type in ReflectionApi.GetAllTypesEnumerator())
                {
                    if (type.IsAbstract == false && type.IsSubclassOf(typeof(CsvDataBase)))
                    {
                        var constructor = type.GetConstructor(Type.EmptyTypes);
                        var csvObj = (CsvDataBase)constructor.Invoke(null);
                        var dataGroup = csvObj.GetDataGroup();
                        if (dataGroup != null)
                        {
                            if (ResourceApi.DataGroupCsvPairs.ContainsKey(dataGroup) == false)
                                ResourceApi.DataGroupCsvPairs[dataGroup] = new();
                            ResourceApi.DataGroupCsvPairs[dataGroup].Add(csvObj);
                        }
                    }
                }
                // init resource
                ResourceApi.Initialize();
                DebugApi.Log(ResourceManager.ToString());
            }

            public void Unload(GameStage stage)
            {
            }
        }

        private class LoadLocTranslations : IStageLoad
        {
            public void Load(GameStage stage)
            {
                LocApi.LoadCsvByNameFunction = ResourceApi.LoadLocKeyTable;
                if (LocApi.GetLanguageList().Count == 0)
                    return;
                var currentId = LocApi.GetCurrentLanguage();
                if (string.IsNullOrEmpty(currentId))
                    currentId = LocApi.GetLanguageList()[0].Id;
                LocApi.SetCurrentLanguage(currentId);
            }

            public void Unload(GameStage stage)
            {
                LocApi.SetCurrentLanguage(null);
                LocApi.LoadCsvByNameFunction = null;
            }
        }
    }
}
