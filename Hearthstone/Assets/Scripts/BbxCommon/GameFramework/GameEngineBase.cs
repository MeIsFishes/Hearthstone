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
            public long SetActiveGameStage(params GameStage[] stages) => m_Ref.SetActiveGameStage(stages);
            public GameStageTransitionResult LastTransitionResult => m_Ref.m_LastStageTransitionResult;
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

        /// <summary>
        /// Called after one queued stage-operation batch has fully settled.
        /// New stage-group requests made here are queued as a later batch instead of being
        /// appended to the operation list that has just completed.
        /// </summary>
        protected virtual void OnStageLoadingCompleted(IReadOnlyList<GameStage> activeStages) { }

        /// <summary>
        /// Called exactly once for every settled transition attempt, including rollback.
        /// The compatibility callback above is invoked only for committed results.
        /// </summary>
        protected virtual void OnStageTransitionCompleted(
            GameStageTransitionResult result,
            IReadOnlyList<GameStage> activeStages)
        {
            if (result.IsCommitted)
                OnStageLoadingCompleted(activeStages);
        }

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
        private List<GameStage> m_EnabledStages = new();
        private List<GameStage> m_RequestedStages = new();
        // Framework systems must remain active while business stages are swapped.
        private GameStage m_GameEngineStage;
        private long m_NextStageTransitionAttemptId;
        private long m_PendingStageTransitionAttemptId;
        private GameStageTransitionResult m_LastStageTransitionResult;

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
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));
            var requested = GetMutableRequestedStageSnapshot();
            if (!requested.Contains(stage))
                requested.Add(stage);
            QueueStageSet(requested);
        }

        private void UnloadStage(GameStage stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));
            if (ReferenceEquals(stage, m_GameEngineStage))
                throw new InvalidOperationException("The framework Game Engine Stage cannot be unloaded explicitly.");
            var requested = GetMutableRequestedStageSnapshot();
            requested.Remove(stage);
            QueueStageSet(requested);
        }

        private long SetActiveGameStage(params GameStage[] stages)
        {
            if (stages == null)
                throw new ArgumentNullException(nameof(stages));

            var requested = new List<GameStage>(stages.Length + 1);
            if (m_GameEngineStage != null)
                requested.Add(m_GameEngineStage);
            for (int i = 0; i < stages.Length; i++)
            {
                var stage = stages[i];
                if (stage == null)
                    throw new ArgumentException("A stage group cannot contain null.", nameof(stages));
                if (!requested.Contains(stage))
                    requested.Add(stage);
                else if (!ReferenceEquals(stage, m_GameEngineStage))
                    throw new ArgumentException($"Stage '{stage.StageName}' is duplicated in the requested group.", nameof(stages));
            }

            return QueueStageSet(requested);
        }

        private List<GameStage> GetMutableRequestedStageSnapshot()
        {
            if (m_StageIsDirty && m_RequestedStages.Count > 0)
                return new List<GameStage>(m_RequestedStages);
            return new List<GameStage>(m_EnabledStages);
        }

        private long QueueStageSet(List<GameStage> stages)
        {
            if (StageSetsEqual(m_RequestedStages, stages) && (m_StageIsDirty || m_IsLoading))
                return m_PendingStageTransitionAttemptId;
            if (!m_IsLoading && StageSetsEqual(m_EnabledStages, stages))
                return m_LastStageTransitionResult?.AttemptId ?? 0;

            m_RequestedStages.Clear();
            m_RequestedStages.AddRange(stages);
            m_PendingStageTransitionAttemptId = ++m_NextStageTransitionAttemptId;
            m_StageIsDirty = true;
            return m_PendingStageTransitionAttemptId;
        }

        private static bool StageSetsEqual(IReadOnlyList<GameStage> left, IReadOnlyList<GameStage> right)
        {
            if (left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (!ReferenceEquals(left[i], right[i]))
                    return false;
            }
            return true;
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
            var targetStages = new List<GameStage>(m_RequestedStages);
            var attemptId = m_PendingStageTransitionAttemptId;
            var oldStages = new List<GameStage>(m_EnabledStages);
            var stagesToLoad = new List<GameStage>();
            var stagesToUnload = new List<GameStage>();
            var contexts = new Dictionary<GameStage, GameStageTransitionContext>();
            var preparedStages = new List<GameStage>();
            var suspendedStages = new List<GameStage>();
            var rollbackErrors = new List<Exception>();
            var cleanupErrors = new List<Exception>();
            Exception failure = null;
            var phase = EGameStageTransitionPhase.Validate;
            var failurePhase = EGameStageTransitionPhase.None;
            bool published = false;

            for (int i = 0; i < targetStages.Count; i++)
            {
                if (!targetStages[i].Loaded)
                    stagesToLoad.Add(targetStages[i]);
            }
            for (int i = oldStages.Count - 1; i >= 0; i--)
            {
                if (!targetStages.Contains(oldStages[i]) && !ReferenceEquals(oldStages[i], m_GameEngineStage))
                    stagesToUnload.Add(oldStages[i]);
            }

            // Transitions away from an active business group use the strict contract. The
            // initial engine/bootstrap load stays compatible, but uses this same scheduler.
            bool strict = false;
            for (int i = 0; i < oldStages.Count; i++)
            {
                if (!ReferenceEquals(oldStages[i], m_GameEngineStage))
                {
                    strict = true;
                    break;
                }
            }

            LoadingTimeData loadingTimeData = null;
            try
            {
                loadingTimeData = EnsureLoadingTimeData();
                SetStageLoadingWeight(stagesToLoad, stagesToUnload, loadingTimeData);

                phase = EGameStageTransitionPhase.Validate;
                for (int i = 0; i < stagesToLoad.Count; i++)
                {
                    var context = new GameStageTransitionContext(attemptId, strict)
                    {
                        Phase = phase,
                    };
                    contexts.Add(stagesToLoad[i], context);
                    stagesToLoad[i].ValidateTransition(context);
                }

                phase = EGameStageTransitionPhase.Prepare;
                for (int i = 0; i < stagesToLoad.Count; i++)
                {
                    var stage = stagesToLoad[i];
                    contexts[stage].Phase = phase;
                    preparedStages.Add(stage);
                    stage.PrepareTransition(contexts[stage]);
                }

                phase = EGameStageTransitionPhase.SuspendOld;
                for (int i = 0; i < stagesToUnload.Count; i++)
                {
                    var suspendErrors = new List<Exception>();
                    stagesToUnload[i].Suspend(suspendErrors);
                    suspendedStages.Add(stagesToUnload[i]);
                    if (suspendErrors.Count > 0)
                        throw new AggregateException($"Stage '{stagesToUnload[i].StageName}' could not be suspended.", suspendErrors);
                }

                phase = EGameStageTransitionPhase.CommitTargetHidden;
                for (int i = 0; i < stagesToLoad.Count; i++)
                {
                    var stage = stagesToLoad[i];
                    contexts[stage].Phase = phase;
                    await stage.CommitTransitionHidden(contexts[stage]);
                }

                phase = EGameStageTransitionPhase.PublishTarget;
                for (int i = 0; i < stagesToLoad.Count; i++)
                {
                    var stage = stagesToLoad[i];
                    contexts[stage].Phase = phase;
                    stage.PublishTransition(contexts[stage]);
                }

                // Publication of the entire group is atomic from the engine's public view.
                m_EnabledStages.Clear();
                m_EnabledStages.AddRange(targetStages);
                published = true;
                for (int i = 0; i < stagesToLoad.Count; i++)
                    stagesToLoad[i].CompleteTransition(contexts[stagesToLoad[i]]);

                phase = EGameStageTransitionPhase.UnloadOld;
                for (int i = 0; i < stagesToUnload.Count; i++)
                {
                    var stage = stagesToUnload[i];
                    var key = stage.StageName + ".Unload";
                    var sampler = DebugApi.BeginSample(key);
                    try { stage.UnloadBestEffort(cleanupErrors); }
                    finally { sampler.EndSample(); }
                    if (loadingTimeData != null)
                    {
                        GameEngineFacade.SetLoadingWeight(loadingTimeData.GetLoadingTime(key));
#if UNITY_EDITOR
                        loadingTimeData.SetLoadingTime(key, sampler.TimeNs);
#endif
                    }
                    await UniTask.NextFrame();
                }
            }
            catch (Exception exception)
            {
                DebugApi.LogException(exception);
                if (published)
                {
                    // Publication is the commit point. Never report rollback or revive a
                    // potentially half-destroyed old group after crossing it.
                    cleanupErrors.Add(exception);
                }
                else
                {
                    failure = exception;
                    failurePhase = phase;
                    for (int i = preparedStages.Count - 1; i >= 0; i--)
                    {
                        var stage = preparedStages[i];
                        stage.RollbackTransition(contexts[stage], rollbackErrors);
                    }
                    for (int i = suspendedStages.Count - 1; i >= 0; i--)
                        suspendedStages[i].Resume(rollbackErrors);
                }
            }

            var result = new GameStageTransitionResult
            {
                AttemptId = attemptId,
                Status = failure != null
                    ? EGameStageTransitionStatus.RolledBack
                    : cleanupErrors.Count > 0
                        ? EGameStageTransitionStatus.CommittedWithCleanupErrors
                        : EGameStageTransitionStatus.Committed,
                FailurePhase = failurePhase,
                Failure = failure,
                RollbackErrors = rollbackErrors.ToArray(),
                CleanupErrors = cleanupErrors.ToArray(),
            };
            m_LastStageTransitionResult = result;

            m_LoadingController?.Hide();
#if UNITY_EDITOR
            if (loadingTimeData != null)
                ResourceApi.EditorOperation.SetDirtyAndSave(loadingTimeData);
#endif
            m_IsLoading = false;
            OnStageTransitionCompleted(result, m_EnabledStages);
            if (m_StageIsDirty)
                OnUpdateStage();
        }

        private LoadingTimeData EnsureLoadingTimeData()
        {
            var loadingTimeData = DataApi.GetData<LoadingTimeData>();
            if (loadingTimeData != null)
                return loadingTimeData;

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
            return loadingTimeData;
        }

        private void SetStageLoadingWeight(
            IReadOnlyList<GameStage> stagesToLoad,
            IReadOnlyList<GameStage> stagesToUnload,
            LoadingTimeData loadingTimeData)
        {
            long totalLoadingTime = 0;
            foreach (var stage in stagesToLoad)
            {
                foreach (var pair in loadingTimeData.LoadingItemTimeDic)
                {
                    if (pair.Key.StartsWith(stage.StageName + ".Load"))
                        totalLoadingTime += pair.Value;
                }
            }
            foreach (var stage in stagesToUnload)
            {
                foreach (var pair in loadingTimeData.LoadingItemTimeDic)
                {
                    if (pair.Key.StartsWith(stage.StageName + ".Unload"))
                        totalLoadingTime += pair.Value;
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
