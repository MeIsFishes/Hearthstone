namespace BbxCommon
{
    [TaskComment("Run another Task graph with the current TaskContext and wait for it to finish.")]
    public class TaskNodeRunTask : TaskBase
    {
        public string TaskKey;
        [TaskComment("Start the child graph in the current TaskSystem tick with a zero-delta first update.")]
        public bool StartInCurrentTick;

        private TaskBase m_RunningTask;
        private bool m_Started;
        private bool m_Finished;
        private bool m_Succeeded;

        public enum EField
        {
            TaskKey,
            StartInCurrentTick,
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.TaskKey, TaskKey, (fieldInfo, context) => { TaskKey = ReadString(fieldInfo, context); });
            RegisterField(EField.StartInCurrentTick, StartInCurrentTick,
                (fieldInfo, context) => { StartInCurrentTick = ReadBool(fieldInfo, context); });
        }

        protected override void OnEnter()
        {
            m_RunningTask = null;
            m_Started = false;
            m_Finished = false;
            m_Succeeded = false;

            if (string.IsNullOrEmpty(TaskKey))
            {
                DebugApi.LogError("RunTask node requires a non-empty TaskKey.");
                m_Finished = true;
                m_Succeeded = false;
                return;
            }

            m_RunningTask = TaskApi.CreateTask(TaskKey, TaskContext);
            if (m_RunningTask == null)
            {
                m_Finished = true;
                m_Succeeded = false;
                return;
            }

            m_RunningTask.OnFinished += OnChildTaskFinished;
            m_Started = true;
            if (StartInCurrentTick)
                m_RunningTask.RunInCurrentTaskTick();
            else
                m_RunningTask.Run();
        }

        protected override ETaskRunState OnUpdate(float deltaTime)
        {
            if (m_Finished == false)
            {
                return ETaskRunState.Running;
            }

            return m_Succeeded ? ETaskRunState.Succeeded : ETaskRunState.Failed;
        }

        protected override void OnExit()
        {
            UnsubscribeFromRunningTask();
        }

        protected override void OnTaskCollect()
        {
            UnsubscribeFromRunningTask();
            TaskKey = string.Empty;
            StartInCurrentTick = false;
            m_RunningTask = null;
            m_Started = false;
            m_Finished = false;
            m_Succeeded = false;
        }

        private void OnChildTaskFinished()
        {
            m_Succeeded = m_RunningTask != null && m_RunningTask.State == ETaskRunState.Succeeded;
            m_Finished = true;
            UnsubscribeFromRunningTask();
        }

        private void UnsubscribeFromRunningTask()
        {
            if (m_Started && m_RunningTask != null)
                m_RunningTask.OnFinished -= OnChildTaskFinished;
            m_Started = false;
        }
    }
}
