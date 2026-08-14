using UnityEngine;

namespace BbxCommon
{
    internal class GameObjectRawComponent : EcsRawComponent
    {
        public GameObject GameObject;

        protected override void OnComponentCollect()
        {
            GameObject = null;
        }
    }
}
