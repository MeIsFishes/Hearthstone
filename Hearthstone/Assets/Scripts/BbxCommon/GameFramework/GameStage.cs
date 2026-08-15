using System;
using System.Collections.Generic;
using BbxCommon.Internal;
using BbxCommon.Ui;
using Cysharp.Threading.Tasks;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace BbxCommon
{
    public interface IStageLoad
    {
        void Load(GameStage stage);
        void Unload(GameStage stage);
    }

    /// <summary>
    /// Opts a stage item into the strict transition contract. Validate and Prepare may not
    /// publish shared state. Load is the hidden commit. Rollback releases prepared and
    /// committed state owned by the item.
    /// </summary>
    public interface ITransactionalStageLoad : IStageLoad
    {
        void Validate(GameStage stage, GameStageTransitionContext context);
        void Prepare(GameStage stage, GameStageTransitionContext context);
        void Rollback(GameStage stage, GameStageTransitionContext context);
    }

    public enum EGameStageTransitionPhase
    {
        None,
        Validate,
        Prepare,
        SuspendOld,
        CommitTargetHidden,
        PublishTarget,
        UnloadOld,
        Complete,
    }

    public enum EGameStageTransitionStatus
    {
        Committed,
        CommittedWithCleanupErrors,
        RolledBack,
    }

    public sealed class GameStageTransitionContext
    {
        private readonly List<Action> m_Compensations = new();

        public long AttemptId { get; }
        public bool Strict { get; }
        public EGameStageTransitionPhase Phase { get; internal set; }

        internal GameStageTransitionContext(long attemptId, bool strict)
        {
            AttemptId = attemptId;
            Strict = strict;
        }

        public void RegisterCompensation(Action compensation)
        {
            if (compensation == null)
                throw new ArgumentNullException(nameof(compensation));
            if (Phase != EGameStageTransitionPhase.Prepare &&
                Phase != EGameStageTransitionPhase.CommitTargetHidden &&
                Phase != EGameStageTransitionPhase.PublishTarget)
            {
                throw new InvalidOperationException($"Compensation cannot be registered during {Phase}.");
            }
            m_Compensations.Add(compensation);
        }

        internal void RollbackCompensations(List<Exception> errors)
        {
            for (int i = m_Compensations.Count - 1; i >= 0; i--)
            {
                try { m_Compensations[i](); }
                catch (Exception exception) { errors.Add(exception); }
            }
            m_Compensations.Clear();
        }

        internal void Commit() => m_Compensations.Clear();
    }

    public sealed class GameStageTransitionResult
    {
        public long AttemptId { get; internal set; }
        public EGameStageTransitionStatus Status { get; internal set; }
        public EGameStageTransitionPhase FailurePhase { get; internal set; }
        public Exception Failure { get; internal set; }
        public IReadOnlyList<Exception> RollbackErrors { get; internal set; } = Array.Empty<Exception>();
        public IReadOnlyList<Exception> CleanupErrors { get; internal set; } = Array.Empty<Exception>();

        public bool IsCommitted =>
            Status == EGameStageTransitionStatus.Committed ||
            Status == EGameStageTransitionStatus.CommittedWithCleanupErrors;
    }

    public class GameStage
    {
        public string StageName;

        protected bool m_Loaded;
        internal bool Loaded => m_Loaded;

        public UnityAction PreLoadStage;
        public UnityAction PostLoadStage;
        public UnityAction PreUnloadStage;
        public UnityAction PostUnloadStage;
        public float StageLoadingWeight = 1f;

        protected World m_EcsWorld;

        private GameStageTransitionContext m_TransitionContext;
        public GameStageTransitionContext ActiveTransitionContext => m_TransitionContext;
        private int m_PreparedLoadItemCount;
        private int m_PreparedLateLoadItemCount;
        private int m_PublishedListenerCount;
        private bool m_UiCreated;
        private bool m_UiSceneWasActive;
        private bool m_Suspended;
        private bool m_SuspendedUiWasActive;

        internal GameStage(string stageName, World ecsWorld)
        {
            StageName = stageName;
            m_EcsWorld = ecsWorld;
        }

        internal void Init(string stageName, World ecsWorld)
        {
            StageName = stageName;
            m_EcsWorld = ecsWorld;
        }

        internal void ValidateTransition(GameStageTransitionContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (m_Loaded)
                throw new InvalidOperationException($"Stage '{StageName}' is already loaded.");
            if (m_TransitionContext != null)
                throw new InvalidOperationException($"Stage '{StageName}' is already participating in a transition.");

            ValidateItems(m_StageLoadItems, context);
            ValidateItems(m_StageLateLoadItems, context);

            if (!context.Strict)
                return;
            if (m_Scenes.Count > 0)
            {
                throw new NotSupportedException(
                    $"Stage '{StageName}' contains Unity scenes. Strict transitions require an inactive-scene staging adapter.");
            }
            if (m_LoadDataGroups.Count > 0)
            {
                throw new NotSupportedException(
                    $"Stage '{StageName}' contains data groups. Strict transitions require a DataApi overlay adapter.");
            }
            if (PreLoadStage != null || PostLoadStage != null)
            {
                throw new NotSupportedException(
                    $"Stage '{StageName}' uses legacy load callbacks which cannot declare transactional safety.");
            }
        }

        private void ValidateItems(IReadOnlyList<IStageLoad> items, GameStageTransitionContext context)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                    throw new InvalidOperationException($"Stage '{StageName}' contains a null load item at index {i}.");
                if (item is ITransactionalStageLoad transactional)
                    transactional.Validate(this, context);
                else if (context.Strict)
                {
                    throw new InvalidOperationException(
                        $"Stage item '{item.GetType().FullName}' in '{StageName}' does not implement {nameof(ITransactionalStageLoad)}.");
                }
            }
        }

        internal void PrepareTransition(GameStageTransitionContext context)
        {
            m_TransitionContext = context ?? throw new ArgumentNullException(nameof(context));
            m_PreparedLoadItemCount = 0;
            m_PreparedLateLoadItemCount = 0;
            m_PublishedListenerCount = 0;
            m_UiCreated = false;

            PrepareItems(m_StageLoadItems, ref m_PreparedLoadItemCount, context);
            PrepareItems(m_StageLateLoadItems, ref m_PreparedLateLoadItemCount, context);
        }

        private void PrepareItems(
            IReadOnlyList<IStageLoad> items,
            ref int preparedCount,
            GameStageTransitionContext context)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is ITransactionalStageLoad transactional)
                    transactional.Prepare(this, context);
                preparedCount++;
            }
        }

        internal async UniTask CommitTransitionHidden(GameStageTransitionContext context)
        {
            EnsureContext(context);

            PreLoadStage?.Invoke();
            await CommitItems(m_StageLoadItems);

            if (m_Scenes.Count > 0)
                OnLoadStageScene();

            if (m_UiScene != null && m_UiSceneAsset != null)
            {
                m_UiSceneWasActive = m_UiScene.gameObject.activeSelf;
                m_UiScene.gameObject.SetActive(false);
            }

            if (m_LoadDataGroups.Count > 0)
                OnLoadStageData();

            await CommitItems(m_StageLateLoadItems);
        }

        private async UniTask CommitItems(IReadOnlyList<IStageLoad> items)
        {
            var loadingTimeData = DataApi.GetData<LoadingTimeData>();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var key = StageName + ".Load." + item.GetType().Name;
                var sampler = DebugApi.BeginSample(key);
                try { item.Load(this); }
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

        internal void PublishTransition(GameStageTransitionContext context)
        {
            EnsureContext(context);

            if (m_UiScene != null && m_UiSceneAsset != null)
            {
                // UiSceneBase currently opens controllers while creating them. Keep the root
                // inactive and perform creation immediately before publication so there is no
                // frame in which a hidden target controller can observe external events.
                m_UiCreated = true;
                m_UiScene.CreateUiByAsset(m_UiSceneAsset);
                m_UiScene.gameObject.SetActive(true);
            }

            PublishSystemsForResume();

            for (int i = 0; i < m_StageListeners.Count; i++)
            {
                try
                {
                    m_StageListeners[i].OnLoad();
                    m_PublishedListenerCount++;
                }
                catch
                {
                    try { m_StageListeners[i].OnUnload(); }
                    catch { }
                    throw;
                }
            }

            PostLoadStage?.Invoke();
            m_Loaded = true;
            m_Suspended = false;
        }

        internal void CompleteTransition(GameStageTransitionContext context)
        {
            if (!ReferenceEquals(m_TransitionContext, context))
                return;
            context.Commit();
            m_TransitionContext = null;
            CleanupObsoleteLoadingTimeEntries();
        }

        internal void RollbackTransition(GameStageTransitionContext context, List<Exception> errors)
        {
            if (!ReferenceEquals(m_TransitionContext, context))
                return;

            for (int i = m_PublishedListenerCount - 1; i >= 0; i--)
            {
                int listenerIndex = i;
                TryCleanup(() => m_StageListeners[listenerIndex].OnUnload(), errors);
            }
            m_PublishedListenerCount = 0;
            TryCleanup(RemovePublishedSystems, errors);
            DestroyTargetSystemsBestEffort(errors);

            if (m_UiCreated)
                UnloadStageUiSceneBestEffort(errors);

            context.RollbackCompensations(errors);
            RollbackPreparedItems(m_StageLateLoadItems, m_PreparedLateLoadItemCount, errors);
            RollbackPreparedItems(m_StageLoadItems, m_PreparedLoadItemCount, errors);

            if (m_UiScene != null)
                TryCleanup(() => m_UiScene.gameObject.SetActive(m_UiSceneWasActive), errors);

            if (!context.Strict)
            {
                if (m_LoadDataGroups.Count > 0)
                    TryCleanup(OnUnloadStageData, errors);
                if (m_Scenes.Count > 0)
                    TryCleanup(OnUnloadStageScene, errors);
            }

            m_Loaded = false;
            m_TransitionContext = null;
        }

        private void RollbackPreparedItems(IReadOnlyList<IStageLoad> items, int preparedCount, List<Exception> errors)
        {
            for (int i = preparedCount - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item is ITransactionalStageLoad transactional)
                    TryCleanup(() => transactional.Rollback(this, m_TransitionContext), errors);
                else
                    TryCleanup(() => item.Unload(this), errors);
            }
        }

        internal void Suspend(List<Exception> errors)
        {
            if (!m_Loaded || m_Suspended)
                return;

            for (int i = m_StageListeners.Count - 1; i >= 0; i--)
            {
                int listenerIndex = i;
                TryCleanup(() => m_StageListeners[listenerIndex].OnUnload(), errors);
            }
            TryCleanup(RemovePublishedSystems, errors);

            if (m_UiScene != null)
            {
                m_SuspendedUiWasActive = m_UiScene.gameObject.activeSelf;
                TryCleanup(() => m_UiScene.gameObject.SetActive(false), errors);
            }
            m_Suspended = true;
        }

        internal void Resume(List<Exception> errors)
        {
            if (!m_Loaded || !m_Suspended)
                return;

            if (m_UiScene != null)
                TryCleanup(() => m_UiScene.gameObject.SetActive(m_SuspendedUiWasActive), errors);
            TryCleanup(PublishSystemsForResume, errors);
            for (int i = 0; i < m_StageListeners.Count; i++)
            {
                int listenerIndex = i;
                TryCleanup(() => m_StageListeners[listenerIndex].OnLoad(), errors);
            }
            m_Suspended = false;
        }

        internal void UnloadBestEffort(List<Exception> errors)
        {
            if (!m_Loaded)
                return;

            TryCleanup(() => PreUnloadStage?.Invoke(), errors);
            for (int i = m_StageLateLoadItems.Count - 1; i >= 0; i--)
            {
                int itemIndex = i;
                TryCleanup(() => m_StageLateLoadItems[itemIndex].Unload(this), errors);
            }
            for (int i = m_StageListeners.Count - 1; i >= 0; i--)
            {
                int listenerIndex = i;
                TryCleanup(() => m_StageListeners[listenerIndex].OnUnload(), errors);
            }
            TryCleanup(RemovePublishedSystems, errors);
            TryCleanup(OnUnloadStageData, errors);
            UnloadStageUiSceneBestEffort(errors);
            TryCleanup(OnUnloadStageScene, errors);
            for (int i = m_StageLoadItems.Count - 1; i >= 0; i--)
            {
                int itemIndex = i;
                TryCleanup(() => m_StageLoadItems[itemIndex].Unload(this), errors);
            }

            m_Loaded = false;
            m_Suspended = false;
            TryCleanup(() => PostUnloadStage?.Invoke(), errors);
        }

        private void EnsureContext(GameStageTransitionContext context)
        {
            if (!ReferenceEquals(m_TransitionContext, context))
                throw new InvalidOperationException($"Stage '{StageName}' does not own transition {context.AttemptId}.");
        }

        private static void TryCleanup(Action action, List<Exception> errors)
        {
            try { action(); }
            catch (Exception exception) { errors.Add(exception); }
        }

        private void RemovePublishedSystems()
        {
            foreach (var system in m_UpdateSystems)
                system.Enabled = false;
            foreach (var system in m_FixedUpdateSystems)
                system.Enabled = false;

            var updateGroup = m_EcsWorld.GetExistingSystemManaged<UpdateSystemGroup>();
            if (updateGroup != null)
                updateGroup.RemoveSystemsFromUpdateOrder(m_UpdateSystems);
            var fixedGroup = m_EcsWorld.GetExistingSystemManaged<FixedUpdateSystemGroup>();
            if (fixedGroup != null)
                fixedGroup.RemoveSystemsFromUpdateOrder(m_FixedUpdateSystems);
        }

        private void PublishSystemsForResume()
        {
            var updateGroup = m_EcsWorld.GetExistingSystemManaged<UpdateSystemGroup>();
            foreach (var system in m_UpdateSystems)
            {
                updateGroup.AddSystemToUpdateList(system);
                system.Enabled = true;
            }
            updateGroup.RefreshSystemUpdateOrder();

            var fixedGroup = m_EcsWorld.GetExistingSystemManaged<FixedUpdateSystemGroup>();
            foreach (var system in m_FixedUpdateSystems)
            {
                fixedGroup.AddSystemToUpdateList(system);
                system.Enabled = true;
            }
            fixedGroup.RefreshSystemUpdateOrder();
        }

        private void DestroyTargetSystemsBestEffort(List<Exception> errors)
        {
            for (int i = m_FixedUpdateSystems.Count - 1; i >= 0; i--)
            {
                int systemIndex = i;
                TryCleanup(() => m_EcsWorld.DestroySystemManaged(m_FixedUpdateSystems[systemIndex]), errors);
            }
            m_FixedUpdateSystems.Clear();

            for (int i = m_UpdateSystems.Count - 1; i >= 0; i--)
            {
                int systemIndex = i;
                TryCleanup(() => m_EcsWorld.DestroySystemManaged(m_UpdateSystems[systemIndex]), errors);
            }
            m_UpdateSystems.Clear();
        }

        internal async UniTask LoadStage()
        {
            if (m_Loaded)
                return;

            var context = new GameStageTransitionContext(0, false)
            {
                Phase = EGameStageTransitionPhase.Validate,
            };
            var errors = new List<Exception>();
            try
            {
                ValidateTransition(context);
                context.Phase = EGameStageTransitionPhase.Prepare;
                PrepareTransition(context);
                context.Phase = EGameStageTransitionPhase.CommitTargetHidden;
                await CommitTransitionHidden(context);
                context.Phase = EGameStageTransitionPhase.PublishTarget;
                PublishTransition(context);
                CompleteTransition(context);
            }
            catch
            {
                RollbackTransition(context, errors);
                throw;
            }
        }

        internal void UnloadStage()
        {
            var errors = new List<Exception>();
            UnloadBestEffort(errors);
            if (errors.Count > 0)
                throw new AggregateException($"Stage '{StageName}' did not unload cleanly.", errors);
        }

        private void CleanupObsoleteLoadingTimeEntries()
        {
#if UNITY_EDITOR
            var loadingTimeData = DataApi.GetData<LoadingTimeData>();
            if (loadingTimeData == null)
                return;
            var stageItems = loadingTimeData.GetStageItemDic(StageName);
            var visitedItems = SimplePool<HashSet<string>>.Alloc();
            visitedItems.Add(StageName + ".Load.Scene");
            visitedItems.Add(StageName + ".Load.UI");
            visitedItems.Add(StageName + ".Load.Data");
            visitedItems.Add(StageName + ".Load.Other");
            visitedItems.Add(StageName + ".Unload");
            foreach (var item in m_StageLoadItems)
                visitedItems.Add(StageName + ".Load." + item.GetType().Name);
            foreach (var item in m_StageLateLoadItems)
                visitedItems.Add(StageName + ".Load." + item.GetType().Name);
            foreach (var pair in stageItems)
            {
                if (!visitedItems.Contains(pair.Key))
                    loadingTimeData.LoadingItemTimeDic.TryRemove(pair.Key);
            }
            visitedItems.CollectToPool();
#endif
        }

        protected Dictionary<string, object> m_StageDatas = new();

        public void SetStageData(string key, object value, bool collectToPool = false)
        {
            if (collectToPool && m_StageDatas.TryGetValue(key, out var origin))
                ((PooledObject)origin)?.CollectToPool();
            m_StageDatas[key] = value;
        }

        public object GetStageData(string key)
        {
            m_StageDatas.TryGetValue(key, out var value);
            return value;
        }

        protected List<string> m_Scenes = new();
        public void AddScene(params string[] scenes) => m_Scenes.AddArray(scenes);

        private void OnLoadStageScene()
        {
            foreach (var scene in m_Scenes)
            {
                if (!scene.IsNullOrEmpty())
                    SceneManager.LoadScene(scene, LoadSceneMode.Additive);
            }
        }

        private void OnUnloadStageScene()
        {
            foreach (var scene in m_Scenes)
            {
                if (!scene.IsNullOrEmpty() && SceneManager.GetSceneByName(scene).isLoaded)
                    SceneManager.UnloadSceneAsync(scene);
            }
        }

        protected UiSceneBase m_UiScene;
        protected UiSceneAsset m_UiSceneAsset;

        public void SetUiScene(UiSceneBase uiScene, UiSceneAsset uiSceneAsset)
        {
            if (m_UiScene != null)
            {
                DebugApi.LogError("Current stage \"" + StageName + "\" has got a UiScene, you can only call SetUiScene once!");
                return;
            }
            m_UiScene = uiScene;
            m_UiSceneAsset = Object.Instantiate(uiSceneAsset);
        }

        private void UnloadStageUiSceneBestEffort(List<Exception> errors)
        {
            if (m_UiSceneAsset != null)
            {
                foreach (var data in m_UiSceneAsset.UiObjectDatas)
                {
                    var controller = data.CreatedController;
                    if (controller == null)
                        continue;
                    TryCleanup(controller.Close, errors);
                    data.CreatedController = null;
                }
            }
            m_UiCreated = false;
        }

        protected List<IStageLoad> m_StageLoadItems = new();
        protected List<IStageLoad> m_StageLateLoadItems = new();

        public void AddLoadItem(IStageLoad item) => m_StageLoadItems.Add(item);
        public void AddLoadItem<T>() where T : IStageLoad, new() => AddLoadItem(new T());
        public void AddLateLoadItem(IStageLoad item) => m_StageLateLoadItems.Add(item);
        public void AddLateLoadItem<T>() where T : IStageLoad, new() => AddLateLoadItem(new T());

        protected List<EcsSystemBase> m_UpdateSystems = new();
        protected List<EcsSystemBase> m_FixedUpdateSystems = new();

        internal IReadOnlyList<EcsSystemBase> UpdateSystems => m_UpdateSystems;
        internal IReadOnlyList<EcsSystemBase> FixedUpdateSystems => m_FixedUpdateSystems;

        public void AddUpdateSystem<T>() where T : EcsSystemBase, new()
        {
            var system = m_EcsWorld.CreateSystemManaged<T>();
            system.Enabled = false;
            m_UpdateSystems.Add(system);
        }

        public void AddFixedUpdateSystem<T>() where T : EcsSystemBase, new()
        {
            var system = m_EcsWorld.CreateSystemManaged<T>();
            system.Enabled = false;
            m_FixedUpdateSystems.Add(system);
        }

        private List<StageListenerBase> m_StageListeners = new();
        public void AddStageListener<T>() where T : StageListenerBase, new() => m_StageListeners.Add(new T());

        private List<string> m_LoadDataGroups = new();
        private HashSet<BbxScriptableObject> m_ScriptableObjects = new();
        public void AddDataGroup(string group) => m_LoadDataGroups.Add(group);

        private void OnLoadStageData()
        {
            var soAssets = Resources.Load<ScriptableObjectAssets>(BbxVar.ExportScriptableObjectPathInResource);
            if (soAssets == null)
                return;
            for (int i = 0; i < m_LoadDataGroups.Count; i++)
            {
                var group = m_LoadDataGroups[i];
                if (soAssets.Assets.TryGetValue(group, out var paths))
                {
                    foreach (var path in paths)
                    {
                        var target = Resources.Load(ResourceApi.EditorOperation.RelativePathToResourcesPath(path));
                        if (target is BbxScriptableObject asset)
                        {
                            var runtimeAsset = Object.Instantiate(asset);
                            runtimeAsset.Load();
                            m_ScriptableObjects.TryAdd(runtimeAsset);
                        }
                    }
                }
                if (ResourceApi.DataGroupCsvPairs.TryGetValue(group, out var csvDataList))
                {
                    foreach (var csvObj in csvDataList)
                    {
                        bool foundTable = false;
                        foreach (var name in csvObj.GetTableNames())
                        {
                            if (ResourceManager.LoadCsv(name, csvObj))
                                foundTable = true;
                        }
                        if (!foundTable)
                        {
                            DebugApi.LogWarning("There is no CSV file fits " + csvObj.GetType().FullName +
                                                " requires. It requires " + csvObj.GetTableNames());
                        }
                    }
                }
            }
        }

        private void OnUnloadStageData()
        {
            foreach (var asset in m_ScriptableObjects)
            {
                asset.Unload();
                Object.Destroy(asset);
            }
            m_ScriptableObjects.Clear();
        }
    }
}
