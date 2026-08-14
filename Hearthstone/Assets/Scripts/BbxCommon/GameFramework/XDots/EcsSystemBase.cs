using System.Collections.Generic;
using Unity.Entities;

namespace BbxCommon
{
    /// <summary>
    /// Base system, which is almost identical to Unity DOTS system.
    /// </summary>
    public abstract partial class EcsSystemBase : SystemBase
    {
        private DebugApi.ProfilerData UpdateSampler;
        internal long LastUpdateTimeNs => UpdateSampler?.TimeNs ?? 0;

        protected override sealed void OnCreate()
        {
            UpdateSampler = new DebugApi.ProfilerData
            {
                Key = GetType().FullName,
            };
            OnSystemCreate();
        }

        protected override sealed void OnUpdate()
        {
            UpdateSampler.BeginSample();
            try
            {
                OnSystemUpdate();
            }
            finally
            {
                UpdateSampler.EndSample();
            }
        }

        protected override sealed void OnDestroy()
        {
            OnSystemDestroy();
        }

        protected virtual void OnSystemCreate() { }
        protected virtual void OnSystemUpdate() { }
        protected virtual void OnSystemDestroy() { }
    }

    /// <summary>
    /// Mixed system, supports using <see cref="EcsRawComponent"/> related functions.
    /// </summary>
    public abstract partial class EcsMixSystemBase : EcsSystemBase
    {
        protected T GetSingletonRawComponent<T>() where T : EcsSingletonRawComponent
        {
            return EcsDataManager.GetSingletonRawComponent<T>();
        }

        protected IEnumerable<T> GetEnumerator<T>() where T : EcsData
        {
            return EcsDataList<T>.GetEnumerator();
        }
    }
}
