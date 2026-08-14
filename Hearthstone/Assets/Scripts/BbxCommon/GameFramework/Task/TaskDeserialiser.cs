using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BbxCommon
{
    internal static class TaskDeserialiser
    {
        #region Context Data
        public class ContextData
        {
            public bool Inited;
            public Dictionary<string, Type> FieldTypeDic = new();
            public Dictionary<string, int> FieldStrIndexDic = new();
        }

        private static List<ContextData> m_ContextDataList = new();

        public static ContextData GetContextData(int typeId)
        {
            if (m_ContextDataList.Count <= typeId)
            {
                m_ContextDataList.ModifyCount(typeId + 1);
                for (int i = 0; i < m_ContextDataList.Count; i++)
                {
                    if (m_ContextDataList[i] == null)
                        m_ContextDataList[i] = new();
                }
            }
            return m_ContextDataList[typeId];
        }

        public static ContextData GetContextData<TContext>() where TContext : TaskContextBase
        {
            var typeId = ClassTypeId<TaskContextBase, TContext>.Id;
            if (m_ContextDataList.Count <= typeId)
            {
                m_ContextDataList.ModifyCount(typeId + 1);
                for (int i = 0; i < m_ContextDataList.Count; i++)
                {
                    if (m_ContextDataList[i] == null)
                        m_ContextDataList[i] = new();
                }
            }
            return m_ContextDataList[typeId];
        }

        public static ContextData GetContextData(Type contextType)
        {
            var typeId = ClassTypeId<TaskContextBase>.GetId(contextType);
            if (m_ContextDataList.Count <= typeId)
            {
                m_ContextDataList.ModifyCount(typeId + 1);
                for (int i = 0; i < m_ContextDataList.Count; i++)
                {
                    if (m_ContextDataList[i] == null)
                        m_ContextDataList[i] = new();
                }
            }
            return m_ContextDataList[typeId];
        }

        public static ContextData GetContextData(TaskContextBase context)
        {
            var typeId = ClassTypeId<TaskContextBase>.GetId(context);
            if (m_ContextDataList.Count <= typeId)
            {
                m_ContextDataList.ModifyCount(typeId + 1);
                for (int i = 0; i < m_ContextDataList.Count; i++)
                {
                    if (m_ContextDataList[i] == null)
                        m_ContextDataList[i] = new();
                }
            }
            return m_ContextDataList[typeId];
        }
        #endregion

        #region Task Pool
        public static List<IObjectPoolHandler> TaskPools = new();

        public static IObjectPoolHandler GetTaskPool(int typeId, Type type)
        {
            if (TaskPools.Count <= typeId)
                TaskPools.ModifyCount(typeId + 1);
            if (TaskPools[typeId] == null)
                TaskPools[typeId] = ObjectPool.GetObjectPool(type);
            return TaskPools[typeId];
        }
        #endregion

        #region Task Data
        public class TaskData
        {
            public int CurEnumIndex;
            public Dictionary<string, int> FieldNameEnumDic = new();
        }

        private static List<TaskData> m_TaskDataList = new();

        public static int GetTaskFieldEnum(int typeId, string fieldName)
        {
            var taskData = GetTaskData(typeId);
            if (taskData.FieldNameEnumDic.TryGetValue(fieldName, out var enumValue) == false)
            {
                enumValue = taskData.CurEnumIndex++;
                taskData.FieldNameEnumDic.Add(fieldName, enumValue);
                return enumValue;
            }
            return enumValue;
        }

        private static TaskData GetTaskData(int typeId)
        {
            if (m_TaskDataList.Count <= typeId)
            {
                m_TaskDataList.ModifyCount(typeId + 1);
                for (int i = 0; i < m_TaskDataList.Count; i++)
                {
                    if (m_TaskDataList[i] == null)
                        m_TaskDataList[i] = new();
                }
            }
            return m_TaskDataList[typeId];
        }
        #endregion

        #region Task Type ID
        private static Dictionary<Type, int> m_TaskTypeIdDic = new();

        public static int GetTaskTypeId(Type type)
        {
            if (m_TaskTypeIdDic.TryGetValue(type, out var typeId) == false)
            {
                typeId = ClassTypeId<TaskBase>.GetId(type);
                m_TaskTypeIdDic[type] = typeId;
                return typeId;
            }
            return typeId;
        }
        #endregion
    }

    #region Bridge Structures
    [StructLayout(LayoutKind.Explicit)]
    public struct TaskBridgeConstValue
    {
        [FieldOffset(0)]
        private bool m_BoolValue;
        public bool BoolValue
        {
            get
            {
                return m_BoolValue;
            }
            set
            {
                m_NumberFlag = 0;
                m_BoolValue = value;
            }
        }
        [FieldOffset(0)]
        private char m_CharValue;
        public char CharValue
        {
            get
            {
                if (m_NumberFlag == 1)
                    return m_CharValue;
                return (char)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 1;
                m_CharValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private byte m_ByteValue;
        public byte ByteValue
        {
            get
            {
                if (m_NumberFlag == 2)
                    return m_ByteValue;
                return (byte)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 2;
                m_ByteValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private short m_ShortValue;
        public short ShortValue
        {
            get
            {
                if (m_NumberFlag == 3)
                    return m_ShortValue;
                return (short)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 3;
                m_ShortValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private ushort m_UshortValue;
        public ushort UshortValue
        {
            get
            {
                if (m_NumberFlag == 4)
                    return m_UshortValue;
                return (ushort)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 4;
                m_UshortValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private int m_IntValue;
        public int IntValue
        {
            get
            {
                if (m_NumberFlag == 5)
                    return m_IntValue;
                return (int)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 5;
                m_IntValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private uint m_UintValue;
        public uint UintValue
        {
            get
            {
                if (m_NumberFlag == 6)
                    return m_UintValue;
                return (uint)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 6;
                m_UintValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private long m_LongValue;
        public long LongValue
        {
            get
            {
                if (m_NumberFlag == 7)
                    return m_LongValue;
                return (long)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 7;
                m_LongValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private ulong m_UlongValue;
        public ulong UlongValue
        {
            get
            {
                if (m_NumberFlag == 8)
                    return m_UlongValue;
                return (ulong)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 8;
                m_UlongValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private float m_FloatValue;
        public float FloatValue
        {
            get
            {
                if (m_NumberFlag == 9)
                    return m_FloatValue;
                return (float)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 9;
                m_FloatValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(0)]
        private double m_DoubleValue;
        public double DoubleValue
        {
            get
            {
                if (m_NumberFlag == 10)
                    return m_DoubleValue;
                return m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 10;
                m_DoubleValue = value;
                m_CommonDouble = value;
            }
        }
        [FieldOffset(0)]
        private decimal m_DecimalValue;
        public decimal DecimalValue
        {
            get
            {
                if (m_NumberFlag == 11)
                    return m_DecimalValue;
                return (decimal)m_CommonDouble;
            }
            set
            {
                m_NumberFlag = 11;
                m_DecimalValue = value;
                m_CommonDouble = (double)value;
            }
        }
        [FieldOffset(8)]
        private object m_ObjectValue;
        public object ObjectValue
        {
            get
            {
                return m_ObjectValue;
            }
            set
            {
                m_NumberFlag = 0;
                m_ObjectValue = value;
            }
        }
        [FieldOffset(16)]
        private string m_StringValue;
        public string StringValue
        {
            get
            {
                return m_StringValue;
            }
            set
            {
                m_NumberFlag = 0;
                m_StringValue = value;
            }
        }
        [FieldOffset(24)]
        // 0 - not number, 1 - char, 2 - byte, 3 - short, 4 - ushort, 5 - int, 6 - uint, 7 - long, 8 - ulong, 9 - float, 10 - double, 11 - decimal
        private byte m_NumberFlag;
        [FieldOffset(25)]
        // All number type will be converted to double for common storage, When you read value, it will try to read from specific type first, if not match, then convert from double.
        // Notice that this may cause precision loss for large value.
        private double m_CommonDouble;
    }

    public class TaskBridgeFieldInfo
    {
        public bool Inited;
        public int FieldEnumValue;
        public ETaskFieldValueSource ValueSource;
        public TaskBridgeConstValue ConstValue;

        internal void FromTaskFieldInfo(TaskFieldInfo taskFieldInfo, int taskTypeId)
        {
            FieldEnumValue = TaskDeserialiser.GetTaskFieldEnum(taskTypeId, taskFieldInfo.FieldName);
            ValueSource = taskFieldInfo.ValueSource;
            ConstValue.StringValue = taskFieldInfo.Value;
        }
    }

    public class TaskBridgeValueInfo
    {
        public Type TaskType;
        public int TaskTypeId;
        public List<TaskBridgeFieldInfo> FieldInfos = new();  // Task fields
        public List<TaskBridgeFieldInfo> BlackboardFieldInfos = new(); // for values in blackboard may be changed dynamically, they should be set every time when task runs
        public bool HasCondition;
        public List<int> EnterConditionReferences = new();
        public List<int> ConditionReferences = new();
        public List<int> ExitConditionReferences = new();
        public bool IsTimeline;
        public List<TaskTimelineItemInfo> TimelineItemInfos = new();    // TaskTimeline uses this struct

        internal void FromTaskValueInfo(TaskValueInfo taskValueInfo, Dictionary<int, int> reorderedIndexDic)
        {
            TaskType = ReflectionApi.GetType(taskValueInfo.FullTypeName);
            TaskTypeId = ClassTypeId<TaskBase>.GetId(TaskType);
            for (int i = 0; i < taskValueInfo.FieldInfos.Count; i++)
            {
                var bridgeFieldInfo = new TaskBridgeFieldInfo();
                bridgeFieldInfo.FromTaskFieldInfo(taskValueInfo.FieldInfos[i], TaskTypeId);
                if (bridgeFieldInfo.ValueSource == ETaskFieldValueSource.Blackboard)
                    BlackboardFieldInfos.Add(bridgeFieldInfo);
                else
                    FieldInfos.Add(bridgeFieldInfo);
            }
            EnterConditionReferences = new List<int>(taskValueInfo.EnterConditionReferences);
            for (int i = 0; i < EnterConditionReferences.Count; i++)
            {
                HasCondition = true;
                EnterConditionReferences[i] = reorderedIndexDic[EnterConditionReferences[i]];
            }
            ConditionReferences = new List<int>(taskValueInfo.ConditionReferences);
            for (int i = 0; i < ConditionReferences.Count; i++)
            {
                HasCondition = true;
                ConditionReferences[i] = reorderedIndexDic[ConditionReferences[i]];
            }
            ExitConditionReferences = new List<int>(taskValueInfo.ExitConditionReferences);
            for (int i = 0; i < ExitConditionReferences.Count; i++)
            {
                HasCondition = true;
                ExitConditionReferences[i] = reorderedIndexDic[ExitConditionReferences[i]];
            }
            for (int i = 0; i < taskValueInfo.TimelineItemInfos.Count; i++)
            {
                var info = new TaskTimelineItemInfo();
                info.StartTime = taskValueInfo.TimelineItemInfos[i].StartTime;
                info.Duration = taskValueInfo.TimelineItemInfos[i].Duration;
                info.Id = reorderedIndexDic[taskValueInfo.TimelineItemInfos[i].Id];
                TimelineItemInfos.Add(info);
            }
            // sort timeline items by start time
            if (TimelineItemInfos.Count > 0)
            {
                IsTimeline = true;
                TimelineItemInfos.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));
            }
        }
    }

    public class TaskBridgeGroupInfo
    {
        public int RootTaskId;
        public Type BindingContextType;
        public List<TaskBridgeValueInfo> TaskValueInfos;
        public Dictionary<int, int> ReorderedIndexDic;

        public void FromTaskGroupInfo(TaskGroupInfo taskGroupInfo)
        {
            BindingContextType = ReflectionApi.GetType(taskGroupInfo.BindingContextFullType);
            // re-order tasks, let them be in continuous index and can be hit through list
            ReorderedIndexDic = new(taskGroupInfo.TaskInfos.Count);
            var tempCurIndex = 0;
            foreach (var pair in taskGroupInfo.TaskInfos)
            {
                if (ReorderedIndexDic.ContainsKey(pair.Key) == false)
                    ReorderedIndexDic.Add(pair.Key, tempCurIndex++);
            }
            TaskValueInfos = new List<TaskBridgeValueInfo>(tempCurIndex);
            TaskValueInfos.ModifyCount(tempCurIndex);
            foreach (var pair in taskGroupInfo.TaskInfos)
            {
                var bridgeValueInfo = new TaskBridgeValueInfo();
                bridgeValueInfo.FromTaskValueInfo(pair.Value, ReorderedIndexDic);
                var reorderedIndex = ReorderedIndexDic[pair.Key];
                TaskValueInfos[reorderedIndex] = bridgeValueInfo;
            }
            RootTaskId = ReorderedIndexDic[taskGroupInfo.RootTaskId];
        }
    }
    #endregion
}
