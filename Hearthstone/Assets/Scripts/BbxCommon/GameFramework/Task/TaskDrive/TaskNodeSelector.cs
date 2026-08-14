
using System;
using System.Collections.Generic;
using BbxCommon.Internal;
using UnityEngine;

namespace BbxCommon
{
    [TaskTag(TaskTagAttribute.ESetTag.Override, TaskExportCrossVariable.TaskTagDrive)]
    public class TaskNodeSelector : TaskBase
    {
        public TaskConnectPoint Tasks = new();
        private int m_CurrentIndex = -1;
        private bool m_ChildActive;

        public enum EField
        {
            Tasks,
        }

        protected override void OnEnter()
        {
            m_CurrentIndex = -1;
            m_ChildActive = false;
            TryEnterNextChild(0);
        }

        protected override ETaskRunState OnUpdate(float deltaTime)
        {
            if (Tasks.Tasks.Count == 0 || m_CurrentIndex == -1)
            {
                return ETaskRunState.Failed;
            }

            var childDeltaTime = deltaTime;
            while (m_CurrentIndex >= 0 && m_CurrentIndex < Tasks.Tasks.Count)
            {
                var state = Tasks.Tasks[m_CurrentIndex].Update(childDeltaTime);
                if (state == ETaskRunState.Running)
                    return ETaskRunState.Running;

                var nextIndex = m_CurrentIndex + 1;
                Tasks.Tasks[m_CurrentIndex].Exit();
                m_ChildActive = false;
                if (state == ETaskRunState.Succeeded)
                    return ETaskRunState.Succeeded;
                if (!TryEnterNextChild(nextIndex))
                    return ETaskRunState.Failed;
                // A fallback child entered during this update starts at the current logical
                // time and must not receive the frame time already consumed by its predecessor.
                childDeltaTime = 0f;
            }
            return ETaskRunState.Failed;
        }

        protected override void OnExit()
        {
            if (m_ChildActive && m_CurrentIndex != -1 && Tasks.Tasks.Count > m_CurrentIndex)
            {
                Tasks.Tasks[m_CurrentIndex].Exit();
                m_ChildActive = false;
            }
        }

        protected override void OnTaskCollect()
        {
            Tasks = null;
            m_CurrentIndex = -1;
            m_ChildActive = false;
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.Tasks, Tasks, (fieldInfo, context) => { Tasks = ReadConnectPoint(fieldInfo, context); });
        }

        private bool TryEnterNextChild(int startIndex)
        {
            for (var i = Mathf.Max(0, startIndex); i < Tasks.Tasks.Count; i++)
            {
                if (!Tasks.Tasks[i].CanEnter())
                    continue;
                m_CurrentIndex = i;
                Tasks.Tasks[i].Enter();
                m_ChildActive = true;
                return true;
            }
            m_CurrentIndex = -1;
            return false;
        }
    }
}
