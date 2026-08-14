using System.Collections.Generic;
using Unity.Entities;

namespace BbxCommon
{
    [DisableAutoCreation]
    public partial class TaskSystem : EcsMixSystemBase
    {
        private const int MaxCurrentTickTaskStarts = 1024;

        // A new-entered task will execute OnEnter(), and then execute OnUpdate(deltaTime) once with deltaTime = 0, and turn to normal next frame.
        protected override void OnSystemUpdate()
        {
            var taskManager = TaskManager.Instance;
            if (taskManager.NewEnterTasks.Count == 0 &&
                taskManager.CurrentTickEnterTasks.Count == 0 &&
                taskManager.RunningTasks.Count == 0)
                return;

            var currentTickTaskStartsRemaining = MaxCurrentTickTaskStarts;
            EnterCurrentTickTasks(taskManager, ref currentTickTaskStartsRemaining);
            EnterNewTasks(taskManager);

            // update
            var finishedTaskIndex = SimplePool<List<int>>.Alloc();
            for (int i = 0; i < taskManager.RunningTasks.Count; i++)
            {
                var taskInfo = taskManager.RunningTasks[i];
                var taskState = ETaskRunState.Running;
                switch (taskInfo.State)
                {
                    case TaskManager.ERunningTaskState.NewEnter:
                        taskState = taskInfo.Task.Update(0);
                        taskManager.RunningTasks[i] = new TaskManager.RunningTaskInfo(taskInfo.Task, TaskManager.ERunningTaskState.Keep);
                        break;
                    case TaskManager.ERunningTaskState.Keep:
                        taskState = taskInfo.Task.Update(TimeApi.DeltaTime);
                        break;
                }
                if (taskState == ETaskRunState.Succeeded || taskState == ETaskRunState.Failed)
                    finishedTaskIndex.Add(i);

                // Opted-in TaskNodeRunTask child graphs belong to the same logical Task tick.
                // Enter them now so the dynamically extended loop gives each one a zero-delta
                // first update later in this tick. A budget prevents recursive task graphs
                // from growing the current tick without bound; overflow remains queued.
                EnterCurrentTickTasks(taskManager, ref currentTickTaskStartsRemaining);
            }

            if (taskManager.CurrentTickEnterTasks.Count > 0)
                DebugApi.LogWarning($"Current-tick Task start budget ({MaxCurrentTickTaskStarts}) was exhausted; " +
                                    $"{taskManager.CurrentTickEnterTasks.Count} Task(s) remain queued for the next tick.");

            // exit
            // Since in some extreme cases, running order may cause bugs, we promise that tasks always run in the order of adding.
            for (int i = 0; i < finishedTaskIndex.Count; i++)
            {
                var taskInfo = taskManager.RunningTasks[finishedTaskIndex[i]];
                taskInfo.Task.Exit();
            }
            for (int i = 0; i < finishedTaskIndex.Count; i++)
            {
                var taskInfo = taskManager.RunningTasks[finishedTaskIndex[i]];
                taskInfo.Task.OnFinished?.Invoke();
            }
            for (int i = finishedTaskIndex.Count - 1; i >= 0; i--)
            {
                taskManager.RunningTasks[finishedTaskIndex[i]].Task.CollectToPool();
                taskManager.RunningTasks.RemoveAt(finishedTaskIndex[i]);
            }

            // collect collections
            finishedTaskIndex.CollectToPool();
        }

        private static void EnterNewTasks(TaskManager taskManager)
        {
            for (int i = 0; i < taskManager.NewEnterTasks.Count; i++)
            {
                var task = taskManager.NewEnterTasks[i];
                task.Enter();
                taskManager.RunningTasks.Add(new TaskManager.RunningTaskInfo(
                    task,
                    TaskManager.ERunningTaskState.NewEnter));
            }
            taskManager.NewEnterTasks.Clear();
        }

        private static void EnterCurrentTickTasks(TaskManager taskManager, ref int startsRemaining)
        {
            if (startsRemaining <= 0 || taskManager.CurrentTickEnterTasks.Count == 0)
                return;

            var enterCount = System.Math.Min(startsRemaining, taskManager.CurrentTickEnterTasks.Count);
            for (var i = 0; i < enterCount; i++)
            {
                var task = taskManager.CurrentTickEnterTasks[i];
                task.Enter();
                taskManager.RunningTasks.Add(new TaskManager.RunningTaskInfo(
                    task,
                    TaskManager.ERunningTaskState.NewEnter));
            }
            taskManager.CurrentTickEnterTasks.RemoveRange(0, enterCount);
            startsRemaining -= enterCount;
        }
    }
}
