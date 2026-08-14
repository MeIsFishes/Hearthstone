using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using BbxCommon.Ui;
using Cysharp.Threading.Tasks;
using BbxCommon.Internal;
#if UNITY_EDITOR
using BbxCommon.Editor;
#endif

namespace BbxCommon
{
    #region SystemGroup
    [DisableAutoCreation]
    internal abstract partial class GameEngineOrderedSystemGroup : ComponentSystemGroup
    {
        private readonly List<Type> m_SystemOrder = new();
        private readonly Dictionary<Type, int> m_SystemOrderByType = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            EnableSystemSorting = false;
        }

        internal void SetSystemsInUpdateOrder(IReadOnlyList<EcsSystemBase> systems)
        {
            var currentSystems = new List<ComponentSystemBase>(Systems.Count);
            for (int i = 0; i < Systems.Count; i++)
            {
                currentSystems.Add(Systems[i]);
            }

            foreach (var system in currentSystems)
            {
                RemoveSystemFromUpdateList(system);
            }

            // With DOTS sorting disabled this flushes pending removals without
            // applying UpdateBefore/UpdateAfter attributes.
            SortSystems();

            foreach (var system in systems)
            {
                AddSystemToUpdateList(system);
            }
        }

        internal void SetRegisteredSystemOrder(IReadOnlyList<Type> systemTypes)
        {
            m_SystemOrder.Clear();
            m_SystemOrderByType.Clear();
            for (int i = 0; i < systemTypes.Count; i++)
            {
                m_SystemOrder.Add(systemTypes[i]);
                m_SystemOrderByType.Add(systemTypes[i], i);
            }

            RefreshSystemUpdateOrder();
        }

        internal void RefreshSystemUpdateOrder()
        {
            var systems = new List<EcsSystemBase>(Systems.Count);
            for (int i = 0; i < Systems.Count; i++)
            {
                if (Systems[i] is EcsSystemBase system)
                    systems.Add(system);
            }

            SetSystemsInUpdateOrder(GetSystemsInRegisteredOrder(systems));
        }

        internal void RemoveSystemsFromUpdateOrder(IReadOnlyList<EcsSystemBase> systemsToRemove)
        {
            var systemsToRemoveSet = new HashSet<EcsSystemBase>(systemsToRemove);
            var remainingSystems = new List<EcsSystemBase>(Systems.Count);
            for (int i = 0; i < Systems.Count; i++)
            {
                if (Systems[i] is EcsSystemBase system && !systemsToRemoveSet.Contains(system))
                    remainingSystems.Add(system);
            }

            SetSystemsInUpdateOrder(GetSystemsInRegisteredOrder(remainingSystems));
        }

        private List<EcsSystemBase> GetSystemsInRegisteredOrder(List<EcsSystemBase> systems)
        {
            var orderedSystems = new List<EcsSystemBase>(systems.Count);
            foreach (var registeredType in m_SystemOrder)
            {
                foreach (var system in systems)
                {
                    if (system.GetType() == registeredType)
                        orderedSystems.Add(system);
                }
            }

            foreach (var system in systems)
            {
                if (!m_SystemOrderByType.ContainsKey(system.GetType()))
                    orderedSystems.Add(system);
            }

            return orderedSystems;
        }
    }

    [DisableAutoCreation]
    internal partial class UpdateSystemGroup : GameEngineOrderedSystemGroup { }

    [DisableAutoCreation]
    internal partial class FixedUpdateSystemGroup : GameEngineOrderedSystemGroup { }
    #endregion

    public interface IGameEngine
    {
        List<GameStage> GetEnabledGameStage();
        bool TryGetSystemOrder(Type systemType, out int order);
        bool TryGetSystemExecutionOrder(EcsSystemBase system, out int order);
    }

    internal static class GameEngineFacade
    {
        #region Loading Progress
        public static float LoadingProgress => m_CurLoadingWeight / (float)m_TotalLoadingWeight;

        private static long m_TotalLoadingWeight;
        private static long m_CurLoadingWeight;

        public static void SetTotalLoadingWeight(long weight)
        {
            m_TotalLoadingWeight = weight;
            m_CurLoadingWeight = 0;
        }

        public static void SetLoadingWeight(long weight)
        {
            m_CurLoadingWeight += weight;
            if (m_CurLoadingWeight > m_TotalLoadingWeight)
                m_CurLoadingWeight = m_TotalLoadingWeight;
        }
        #endregion
    }

    public abstract partial class GameEngineBase<TEngine> : MonoSingleton<TEngine>, IGameEngine where TEngine : GameEngineBase<TEngine>
    {
        #region Wrappers
        public EngineUiSceneWp UiSceneWrapper;
        public EngineStageWp StageWrapper;

        private void InitWrapper()
        {
            UiSceneWrapper = new EngineUiSceneWp(this);
            StageWrapper = new EngineStageWp(this);
        }

        public struct EngineUiSceneWp
        {
            private GameEngineBase<TEngine> m_Ref;

            public EngineUiSceneWp(GameEngineBase<TEngine> engine) { m_Ref = engine; }

            public T CreateUiScene<T>() where T : UiSceneBase => m_Ref.CreateUiScene<T>();
            public T GetUiScene<T>() where T : UiSceneBase => m_Ref.GetUiScene<T>();
            public T GetOrCreateUiScene<T>() where T : UiSceneBase => m_Ref.GetOrCreateUiScene<T>();
        }

        public struct EngineStageWp
        {
            private GameEngineBase<TEngine> m_Ref;

            public EngineStageWp(GameEngineBase<TEngine> engine) { m_Ref = engine; }

            public GameStage CreateStage(string stageName) => m_Ref.CreateStage(stageName);
            public GameStage CreateStage<T>(string stageName) where T : GameStage, new() => m_Ref.CreateStage<T>(stageName);
            public void LoadStage(GameStage stage) => m_Ref.LoadStage(stage);
            public void UnloadStage(GameStage stage) => m_Ref.UnloadStage(stage);
            public void SetActiveGameStage(params GameStage[] stages) => m_Ref.SetActiveGameStage(stages);
        }
        #endregion

        #region Unity Callbacks
        protected sealed override void Awake()
        {
            base.Awake();

            DontDestroyOnLoad(this);
            
            InitWrapper();

            OnAwakeEcsWorld();
            OnAwakeUiScene();
            OnAwakeStage();

            // call overridable OnAwake() after all datas are initialized
            OnAwake();

#if UNITY_EDITOR
            GameStageWindow.CurGameEngine = this;
#endif
        }

        protected virtual void OnAwake() { }

        private void Update()
        {
            OnUpdateStage();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            GameStageWindow.CurGameEngine = null;
#endif
        }
        #endregion

        #region UiScene
        public GameObject UiCanvasProto;

        private GameObject m_UiSceneRoot;
        private Dictionary<Type, UiSceneBase> m_UiScenes = new Dictionary<Type, UiSceneBase>();
        private UiControllerBase m_LoadingController;

        public T CreateUiScene<T>() where T : UiSceneBase
        {
            var type = typeof(T);
            var uiSceneName = type.Name;
            var uiSceneGameObject = new GameObject(uiSceneName);
            uiSceneGameObject.transform.SetParent(m_UiSceneRoot.transform);
            var uiScene = uiSceneGameObject.AddComponent<T>();
            uiScene.InitUiScene(UiCanvasProto);
            m_UiScenes.Add(type, uiScene);
            return uiScene;
        }

        public T GetUiScene<T>() where T : UiSceneBase
        {
            m_UiScenes.TryGetValue(typeof(T), out var uiScene);
            return (T)uiScene;
        }

        public T GetOrCreateUiScene<T>() where T : UiSceneBase
        {
            if (m_UiScenes.TryGetValue(typeof(T), out var uiScene))
                return (T)uiScene;
            else
                return CreateUiScene<T>();
        }

        private void OnAwakeUiScene()
        {
            if (UiCanvasProto == null)
                return;
            m_UiSceneRoot = new GameObject("UiSceneRoot");
            m_UiSceneRoot.transform.SetParent(this.transform);

            UiApi.HudRoot = Instantiate(UiCanvasProto);
            UiApi.HudRoot.name = "HudRoot";
            UiApi.HudRoot.transform.SetParent(m_UiSceneRoot.transform);
            UiApi.HudRoot.GetComponent<Canvas>().sortingOrder = -100;

            var customUiSceneRoot = new GameObject("CustomUiScenes");
            customUiSceneRoot.transform.SetParent(m_UiSceneRoot.transform);

            var uiGameEngineScene = CreateUiScene<UiGameEngineScene>();
            UiApi.SetUiGameEngineScene(uiGameEngineScene);
            m_UiSceneRoot = customUiSceneRoot;  // keep custom UiScenes hang on a separate root to ensure GameEngine can show its UI items above other all

            // initialize UI prefab data
            var data = Resources.Load<PreLoadUiData>(BbxVar.ExportPreLoadUiPathInResources);
            if (data != null)
            {
                data = Instantiate(data);   // create a copy
                DataApi.SetData(data);
            }
        }
        #endregion

        #region EcsWorld
        private World m_EcsWorld;
        private Entity m_SingletonEntity;
        private readonly List<Type> m_SystemOrder = new();
        private readonly Dictionary<Type, int> m_SystemOrderByType = new();

        private void OnAwakeEcsWorld()
        {
            m_EcsWorld = World.DefaultGameObjectInjectionWorld;

            var simulationSystemGroup = m_EcsWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();
            var updateSystemGroup = m_EcsWorld.GetOrCreateSystemManaged<UpdateSystemGroup>();
            simulationSystemGroup.AddSystemToUpdateList(updateSystemGroup);

            var fixedStepSystemGroup = m_EcsWorld.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
            var fixedUpdateSystemGroup = m_EcsWorld.GetOrCreateSystemManaged<FixedUpdateSystemGroup>();
            fixedStepSystemGroup.AddSystemToUpdateList(fixedUpdateSystemGroup);

            m_SingletonEntity = EcsApi.CreateEntity();
            EcsDataManager.SetSingletonRawComponentEntity(m_SingletonEntity);
        }

        /// <summary>
        /// Registers the execution order for GameStage-managed ECS systems.
        /// Registered types run first in the supplied order; unregistered types run afterwards.
        /// Update and FixedUpdate systems apply the same type order within their own update groups.
        /// </summary>
        protected void RegisterSystemOrder(params Type[] systemTypes)
        {
            if (systemTypes == null)
                throw new ArgumentNullException(nameof(systemTypes));

            var newSystemTypes = new HashSet<Type>();
            foreach (var systemType in systemTypes)
            {
                if (systemType == null)
                    throw new ArgumentException("System order cannot contain a null type.", nameof(systemTypes));
                if (!typeof(EcsSystemBase).IsAssignableFrom(systemType))
                    throw new ArgumentException($"{systemType.FullName} must inherit {nameof(EcsSystemBase)}.", nameof(systemTypes));
                if (systemType.IsAbstract || systemType.ContainsGenericParameters)
                    throw new ArgumentException($"{systemType.FullName} must be a concrete, closed system type.", nameof(systemTypes));
                if (m_SystemOrderByType.ContainsKey(systemType) || !newSystemTypes.Add(systemType))
                    throw new ArgumentException($"{systemType.FullName} is already registered in the system order.", nameof(systemTypes));
            }

            foreach (var systemType in systemTypes)
            {
                m_SystemOrderByType.Add(systemType, m_SystemOrder.Count);
                m_SystemOrder.Add(systemType);
            }

            m_EcsWorld.GetExistingSystemManaged<UpdateSystemGroup>()
                .SetRegisteredSystemOrder(m_SystemOrder);
            m_EcsWorld.GetExistingSystemManaged<FixedUpdateSystemGroup>()
                .SetRegisteredSystemOrder(m_SystemOrder);
        }
        #endregion

        #region GameStage
        private enum EOperateStage
        {
            Load,
            Unload,
        }

        private struct OperateStage
        {
            public GameStage Stage;
            public EOperateStage OperateType;

            public OperateStage(GameStage stage, EOperateStage operateType)
            {
                Stage = stage;
                OperateType = operateType;
            }
        }

        private List<GameStage> m_EnabledStages = new();
        private List<OperateStage> m_OperateStages = new();
        // Framework systems must remain active while business stages are swapped.
        private GameStage m_GameEngineStage;

        List<GameStage> IGameEngine.GetEnabledGameStage()
        {
            return m_EnabledStages;
        }

        bool IGameEngine.TryGetSystemOrder(Type systemType, out int order)
        {
            return m_SystemOrderByType.TryGetValue(systemType, out order);
        }

        bool IGameEngine.TryGetSystemExecutionOrder(EcsSystemBase system, out int order)
        {
            var updateSystems = m_EcsWorld.GetExistingSystemManaged<UpdateSystemGroup>().Systems;
            for (int i = 0; i < updateSystems.Count; i++)
            {
                if (ReferenceEquals(updateSystems[i], system))
                {
                    order = i;
                    return true;
                }
            }

            var fixedUpdateSystems = m_EcsWorld.GetExistingSystemManaged<FixedUpdateSystemGroup>().Systems;
            for (int i = 0; i < fixedUpdateSystems.Count; i++)
            {
                if (ReferenceEquals(fixedUpdateSystems[i], system))
                {
                    order = i;
                    return true;
                }
            }

            order = -1;
            return false;
        }

    public GameStage CreateStage(string stageName)
        {
            return new GameStage(stageName, m_EcsWorld);
        }

        public T CreateStage<T>(string stageName) where T : GameStage, new()
        {
            var stage = new T();
            stage.Init(stageName, m_EcsWorld);
            return stage;
        }

        private bool m_StageIsDirty;
        private bool m_IsLoading;

        private void LoadStage(GameStage stage)
        {
            m_OperateStages.Add(new OperateStage(stage, EOperateStage.Load));
            m_StageIsDirty = true;
        }

        private void UnloadStage(GameStage stage)
        {
            m_OperateStages.Add(new OperateStage(stage, EOperateStage.Unload));
            m_StageIsDirty = true;
        }

        private void SetActiveGameStage(params GameStage[] stages)
        {
            for (int i = m_EnabledStages.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(m_EnabledStages[i], m_GameEngineStage))
                    continue;

                if (System.Array.IndexOf(stages, m_EnabledStages[i]) < 0)
                    UnloadStage(m_EnabledStages[i]);
            }
            foreach (var stage in stages)
            {
                if (!stage.Loaded)
                    LoadStage(stage);
            }
        }

        private void OnAwakeStage()
        {
            m_GameEngineStage = CreateGameEngineStage();
            LoadStage(m_GameEngineStage);
        }

        private async void StartLoading()
        {
            m_IsLoading = true;
            m_LoadingController?.Show();
            var loadingTimeData = DataApi.GetData<LoadingTimeData>();
            if (loadingTimeData == null)
            {
                loadingTimeData = Resources.Load<LoadingTimeData>(BbxVar.ExportLoadingTimeDataPath);
#if UNITY_EDITOR
                if (loadingTimeData == null)
                {
                    loadingTimeData = ResourceApi.EditorOperation.LoadOrCreateAssetInResources<LoadingTimeData>(
                        BbxVar.ExportLoadingTimeDataPath);
                }
#endif
                if (loadingTimeData == null)
                {
                    throw new InvalidOperationException(
                        $"Required loading-time data was not found at Resources/{BbxVar.ExportLoadingTimeDataPath}.");
                }
                DataApi.SetData(loadingTimeData);
            }
            SetStageLoadingWeight();
            // unload stage
            for (int i = 0; i < m_OperateStages.Count; i++)
            {
                if (m_OperateStages[i].OperateType == EOperateStage.Unload)
                {
                    var stage = m_OperateStages[i].Stage;
                    var key = stage.StageName + ".Unload";
                    var sampler = DebugApi.BeginSample(key);
                    stage.UnloadStage();
                    m_EnabledStages.Remove(stage);
                    sampler.EndSample();
                    GameEngineFacade.SetLoadingWeight(loadingTimeData.GetLoadingTime(key));
#if UNITY_EDITOR
                    loadingTimeData.SetLoadingTime(key, sampler.TimeNs);
#endif
                    await UniTask.NextFrame();
                }
            }
            // load stage
            for (int i = 0; i < m_OperateStages.Count; i++)
            {
                if (m_OperateStages[i].OperateType == EOperateStage.Load)
                {
                    try
                    {
                        await m_OperateStages[i].Stage.LoadStage();
                    }
                    catch (Exception e)
                    {
                        DebugApi.LogException(e);
                    }
                    m_EnabledStages.Add(m_OperateStages[i].Stage);
                }
            }
            m_OperateStages.Clear();
            m_LoadingController?.Hide();
#if UNITY_EDITOR
            ResourceApi.EditorOperation.SetDirtyAndSave(loadingTimeData);
#endif
            m_IsLoading = false;
            if (m_StageIsDirty)
                OnUpdateStage();
        }

        private void SetStageLoadingWeight()
        {
            if (m_OperateStages.Count == 0)
            {
                return;
            }

            var loadingTimeData = DataApi.GetData<LoadingTimeData>();
            long totalLoadingTime = 0;
            foreach (var operate in m_OperateStages)
            {
                var stage = operate.Stage;
                if (operate.OperateType == EOperateStage.Load)
                {
                    foreach (var pair in loadingTimeData.LoadingItemTimeDic)
                    {
                        if (pair.Key.StartsWith(stage.StageName + ".Load"))
                            totalLoadingTime += pair.Value;
                    }
                }
                else if (operate.OperateType == EOperateStage.Unload)
                {
                    foreach (var pair in loadingTimeData.LoadingItemTimeDic)
                    {
                        if (pair.Key.StartsWith(stage.StageName + ".Unload"))
                            totalLoadingTime += pair.Value;
                    }
                }
            }
            GameEngineFacade.SetTotalLoadingWeight(totalLoadingTime);
        }
        
        public void SetLoadingUi<T>() where T : UiControllerBase
        {
            if (m_LoadingController != null && m_LoadingController.GetType() == typeof(T))
                return;
            m_LoadingController?.Close();
            m_LoadingController = UiApi.OpenUiController<T>(UiApi.GetUiGameEngineScene().GetUiGroupCanvas(EUiGameEngine.Loading).transform, false);
        }
        
        private void OnUpdateStage()
        {
            if (m_StageIsDirty && !m_IsLoading)
            {
                m_StageIsDirty = false;
                StartLoading();
            }
        }
        #endregion
    }
}
