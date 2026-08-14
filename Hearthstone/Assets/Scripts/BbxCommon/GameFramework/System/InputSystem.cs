using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using BbxCommon;

namespace BbxCommon
{
    [DisableAutoCreation]
    public partial class InputSystem : EcsMixSystemBase
    {
        protected override void OnSystemUpdate()
        {
            SimplePool.Alloc(out Dictionary<int, bool> keyStates);
            InputApi.GetKeyStateRequestDic(keyStates);
            SimplePool.Alloc(out List<int> keyDownKeys);
            foreach (var pair in keyStates)
            {
                if (Input.GetKeyDown((KeyCode)pair.Key))
                {
                    keyDownKeys.Add(pair.Key);
                }
            }
            for (int i = 0; i < keyDownKeys.Count; i++)
            {
                keyStates[keyDownKeys[i]] = true;
            }
            keyDownKeys.CollectToPool();
            InputApi.Tick(keyStates);
            keyStates.CollectToPool();
        }
    }
}
