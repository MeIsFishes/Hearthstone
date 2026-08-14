using System;
using BbxCommon;
using System.Collections.Generic;
using BbxCommon.Ui;
using BbxCommon.Internal;

namespace BbxCommon
{
    [TaskTag(TaskTagAttribute.ESetTag.Override, TaskExportCrossVariable.TaskTagDrive)]
    public class TaskBtRoot : TaskBase
    {
        public TaskConnectPoint Tasks = new();
        private bool m_ChildActive;

        public enum EField
        {
            Tasks,
        }

        protected override void OnEnter()
        {
            if (Tasks.Tasks.Count == 0)
            {
                return;
            }
            Tasks.Tasks[0].Enter();
            m_ChildActive = true;
        }

        protected override ETaskRunState OnUpdate(float deltaTime)
        {
            if (Tasks.Tasks.Count == 0 || Tasks.Tasks[0] == null)
            {
                return ETaskRunState.Succeeded;
            }
                
            var state = Tasks.Tasks[0].Update(deltaTime);
            return state;
        }

        protected override void OnExit()
        {
            if (!m_ChildActive || Tasks.Tasks.Count == 0 || Tasks.Tasks[0] == null)
                return;
            Tasks.Tasks[0].Exit();
            m_ChildActive = false;
        }

        protected override void OnTaskCollect()
        {
            Tasks = null;
            m_ChildActive = false;
        }

        protected override void RegisterFields() 
        {
            RegisterField(EField.Tasks, Tasks, (fieldInfo, context) => { Tasks = ReadConnectPoint(fieldInfo, context); });
        }
    }
}
