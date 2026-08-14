
namespace BbxCommon
{
    public abstract class EcsRawComponent : EcsData
    {
        protected sealed override void OnAllocate()
        {
            base.OnAllocate();
            OnComponentAllocate();
        }

        protected sealed override void OnCollect()
        {
            OnComponentCollect();
            base.OnCollect();
        }

        protected virtual void OnComponentAllocate() { }
        protected virtual void OnComponentCollect() { }
    }

    public abstract class EcsSingletonRawComponent : EcsRawComponent, IEcsSingletonData
    {
        protected sealed override void OnComponentAllocate()
        {
            OnSingletonAllocate();
        }

        protected sealed override void OnComponentCollect()
        {
            OnSingletonCollect();
        }

        protected virtual void OnSingletonAllocate() { }
        protected virtual void OnSingletonCollect() { }
    }
}
