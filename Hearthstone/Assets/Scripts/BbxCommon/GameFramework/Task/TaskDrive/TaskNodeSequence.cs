
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BbxCommon;
using BbxCommon.Internal;
using UnityEngine;

namespace BbxCommon
{
    [TaskTag(TaskTagAttribute.ESetTag.Override, TaskExportCrossVariable.TaskTagDrive)]
    public class TaskNodeSequence : TaskBase
    {
        public TaskConnectPoint Tasks = new();
        private int m_CurrentIndex;
        private bool m_ChildActive;

        public enum EField
        {
            Tasks,
        }

        protected override void OnEnter()
        {
            m_CurrentIndex = 0;
            m_ChildActive = false;
            if (Tasks.Tasks.Count > 0)
            {
                Tasks.Tasks[m_CurrentIndex].Enter();
                m_ChildActive = true;
            }
        }

        protected override ETaskRunState OnUpdate(float deltaTime)
        {
            if (Tasks.Tasks.Count == 0)
            {
                return ETaskRunState.Succeeded;
            }

            var childDeltaTime = deltaTime;

            while (m_CurrentIndex < Tasks.Tasks.Count)
            {
                var state = Tasks.Tasks[m_CurrentIndex].Update(childDeltaTime);
                
                if (state == ETaskRunState.Running)
                {
                    return ETaskRunState.Running;
                }

                Tasks.Tasks[m_CurrentIndex].Exit();
                m_ChildActive = false;
                if (state == ETaskRunState.Failed)
                    return ETaskRunState.Failed;

                m_CurrentIndex++;
                if (m_CurrentIndex < Tasks.Tasks.Count)
                {
                    Tasks.Tasks[m_CurrentIndex].Enter();
                    m_ChildActive = true;
                    // The frame time was already consumed by the child that just finished.
                    // Newly-entered children may run immediately, but must not consume it again.
                    childDeltaTime = 0f;
                }
            }
            return ETaskRunState.Succeeded;
        }

        protected override void OnExit()
        {
            if (!m_ChildActive || m_CurrentIndex < 0 || m_CurrentIndex >= Tasks.Tasks.Count)
                return;
            Tasks.Tasks[m_CurrentIndex].Exit();
            m_ChildActive = false;
        }

        protected override void OnTaskCollect()
        {
            Tasks = null;
            m_CurrentIndex = 0;
            m_ChildActive = false;
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.Tasks, Tasks, (fieldInfo, context) => { Tasks = ReadConnectPoint(fieldInfo, context); });
        }
    }
}
