
using System.Collections.Generic;
using BbxCommon.Internal;

namespace BbxCommon
{
    /// <summary>
    /// Opt-in rollback for a child that already succeeded and exited while its owning
    /// <see cref="TaskNodeParallel"/> has not yet succeeded as a whole.
    /// </summary>
    public interface ITaskParallelRollback
    {
        void RollbackSucceededResult();
    }

    [TaskTag(TaskTagAttribute.ESetTag.Override, TaskExportCrossVariable.TaskTagDrive)]
    public class TaskNodeParallel : TaskBase
    {
        public TaskConnectPoint Tasks = new();

        private readonly List<bool> m_ChildActive = new();
        private readonly List<bool> m_ChildSucceeded = new();
        private bool m_HasInvalidChild;
        private bool m_AllChildrenSucceeded;

        public enum EField
        {
            Tasks,
        }

        protected override void OnEnter()
        {
            m_ChildActive.Clear();
            m_ChildSucceeded.Clear();
            m_AllChildrenSucceeded = false;
            m_HasInvalidChild = Tasks?.Tasks == null;
            if (m_HasInvalidChild)
            {
                return;
            }

            for (int i = 0; i < Tasks.Tasks.Count; i++)
            {
                var child = Tasks.Tasks[i];
                var isActive = child != null;
                m_ChildActive.Add(isActive);
                m_ChildSucceeded.Add(false);
                if (isActive)
                {
                    child.Enter();
                }
                else
                {
                    m_HasInvalidChild = true;
                }
            }
        }

        protected override ETaskRunState OnUpdate(float deltaTime)
        {
            if (m_HasInvalidChild)
            {
                RollbackSucceededChildren();
                ExitActiveChildren();
                return ETaskRunState.Failed;
            }

            var hasActiveChild = false;
            for (int i = 0; i < Tasks.Tasks.Count; i++)
            {
                if (!m_ChildActive[i])
                {
                    continue;
                }

                var state = Tasks.Tasks[i].Update(deltaTime);
                if (state == ETaskRunState.Running)
                {
                    hasActiveChild = true;
                    continue;
                }

                Tasks.Tasks[i].Exit();
                m_ChildActive[i] = false;
                if (state == ETaskRunState.Failed)
                {
                    RollbackSucceededChildren();
                    ExitActiveChildren();
                    return ETaskRunState.Failed;
                }
                m_ChildSucceeded[i] = true;
            }

            if (hasActiveChild)
                return ETaskRunState.Running;
            m_AllChildrenSucceeded = true;
            return ETaskRunState.Succeeded;
        }

        protected override void OnExit()
        {
            if (!m_AllChildrenSucceeded)
                RollbackSucceededChildren();
            ExitActiveChildren();
        }

        protected override void OnBeforeOwnedTaskInstancesCollect()
        {
            // A root Task owns and collects its complete graph before OnTaskCollect runs.
            // Compensate while opt-in children still retain their runtime snapshots.
            if (!m_AllChildrenSucceeded)
                RollbackSucceededChildren();
            ExitActiveChildren();
        }

        protected override void OnTaskCollect()
        {
            if (!m_AllChildrenSucceeded)
                RollbackSucceededChildren();
            ExitActiveChildren();
            m_ChildActive.Clear();
            m_ChildSucceeded.Clear();
            m_HasInvalidChild = false;
            m_AllChildrenSucceeded = false;
            Tasks = null;
        }

        private void RollbackSucceededChildren()
        {
            if (Tasks?.Tasks == null)
            {
                m_ChildSucceeded.Clear();
                return;
            }

            for (var i = 0; i < m_ChildSucceeded.Count; i++)
            {
                if (!m_ChildSucceeded[i])
                    continue;
                if (i < Tasks.Tasks.Count && Tasks.Tasks[i] is ITaskParallelRollback rollback)
                    rollback.RollbackSucceededResult();
                m_ChildSucceeded[i] = false;
            }
        }

        private void ExitActiveChildren()
        {
            if (Tasks?.Tasks == null)
            {
                m_ChildActive.Clear();
                return;
            }

            for (int i = 0; i < m_ChildActive.Count; i++)
            {
                if (!m_ChildActive[i])
                {
                    continue;
                }

                if (i < Tasks.Tasks.Count)
                {
                    Tasks.Tasks[i]?.Exit();
                }
                m_ChildActive[i] = false;
            }
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.Tasks, Tasks, (fieldInfo, context) => { Tasks = ReadConnectPoint(fieldInfo, context); });
        }
    }
}
