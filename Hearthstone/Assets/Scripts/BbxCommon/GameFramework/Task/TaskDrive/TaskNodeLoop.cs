
using System;
using System.Collections.Generic;
using BbxCommon;
using BbxCommon.Internal;
using Unity.Transforms;
using UnityEngine;

namespace BbxCommon
{
    [TaskTag(TaskTagAttribute.ESetTag.Override, TaskExportCrossVariable.TaskTagDrive)]
    public class TaskNodeLoop: TaskBase
    {
        public TaskConnectPoint Tasks = new();
        public int LoopCount = -1;
        private int m_CurrentCount;
        private bool m_ChildActive;

        public enum EField
        {
            Tasks,
            LoopCount,
        }

        protected override void OnEnter()
        {
            m_CurrentCount = 0;
            m_ChildActive = false;
            if (Tasks.Tasks.Count == 0)
            {
                return;
            }
            Tasks.Tasks[0].Enter();
            m_ChildActive = true;
        }

        protected override ETaskRunState OnUpdate(float deltaTime)
        {
            if (Tasks.Tasks.Count == 0)
                return ETaskRunState.Succeeded;

            var childDeltaTime = deltaTime;
            while (LoopCount < 0 || m_CurrentCount < LoopCount)
            {
                var state = Tasks.Tasks[0].Update(childDeltaTime);
                if (state == ETaskRunState.Running)
                {
                    return ETaskRunState.Running;
                }

                Tasks.Tasks[0].Exit();
                m_ChildActive = false;
                if (state == ETaskRunState.Failed)
                    return ETaskRunState.Failed;
                    
                m_CurrentCount++;
                if (LoopCount > 0 && m_CurrentCount >= LoopCount)
                {
                    return ETaskRunState.Succeeded;
                }

                Tasks.Tasks[0].Enter();
                m_ChildActive = true;
                // A loop iteration may finish and restart in the same parent update, but the
                // next iteration cannot consume the same frame time a second time.
                childDeltaTime = 0f;
                // An infinite loop whose child succeeds immediately must yield instead of
                // spinning forever inside one TaskSystem update.
                if (LoopCount < 0)
                    return ETaskRunState.Running;
            }
            return ETaskRunState.Succeeded;
        }

        protected override void OnExit()
        {
            if (!m_ChildActive || Tasks.Tasks.Count == 0)
                return;
            Tasks.Tasks[0].Exit();
            m_ChildActive = false;
        }

        protected override void OnTaskCollect()
        {
            Tasks = null;
            LoopCount = -1;
            m_CurrentCount = 0;
            m_ChildActive = false;
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.Tasks, Tasks, (fieldInfo, context) => { Tasks = ReadConnectPoint(fieldInfo, context); });
            RegisterField(EField.LoopCount, LoopCount, (fieldInfo, context) => { LoopCount = ReadInt(fieldInfo, context); });
        }
    }
}
