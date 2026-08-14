using System.Collections.Generic;

namespace BbxCommon.Internal
{
    public enum ETaskConnectPointType
    {
        Single,
        Multiple,
    }

    public class TaskConnectPoint
    {
        public ETaskConnectPointType ConnectPointType = ETaskConnectPointType.Multiple;
        public List<TaskBase> Tasks = new();
        internal List<int> TaskRefrenceIds = new();

        public void Reset()
        {
            // A connect point only references nodes owned by the root task graph.
            // The root's owned-instance list is solely responsible for returning
            // those nodes to their pools.
            Tasks.Clear();
            TaskRefrenceIds.Clear();
        }
    }
}
