using BbxCommon.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace BbxCommon
{
    public enum ETaskFieldValueSource
    {
        Value,
        Context,
        Blackboard,
    }

    public struct TaskFieldInfo
    {
        public string FieldName;
        public ETaskFieldValueSource ValueSource;
        public string Value;
    }

    public struct TaskTimelineItemInfo
    {
        public float StartTime;
        public float Duration;
        public int Id;
    }

    public class TaskValueInfo
    {
        public string FullTypeName;
        public List<TaskFieldInfo> FieldInfos = new();  // Task fields
        public List<int> EnterConditionReferences = new();
        public List<int> ConditionReferences = new();
        public List<int> ExitConditionReferences = new();
        public List<TaskTimelineItemInfo> TimelineItemInfos = new();    // TaskTimeline uses this struct

        #region Add Field Info
        public void AddFieldInfo(string fieldName, ETaskFieldValueSource valueSource, string value)
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldName;
            fieldInfo.ValueSource = valueSource;
            fieldInfo.Value = value;
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, bool value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, short value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, ushort value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, int value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, uint value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, long value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, ulong value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, float value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, double value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, string value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value;
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField, TEnum>(TTaskField fieldEnum, TEnum value)
            where TTaskField : Enum where TEnum : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = value.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField, TElement>(TTaskField fieldEnum, List<TElement> value)
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            var sb = new StringBuilder();
            for (int i = 0; i < value.Count; i++)
            {
                sb.Append(value[i].ToString());
                sb.Append(TaskExportCrossVariable.ListElementSplit);
            }
            fieldInfo.Value += sb.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField, TKey, TValue>(TTaskField fieldEnum, Dictionary<TKey, TValue> value)
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Value;
            fieldInfo.Value = JsonApi.SerializeToString(value);
            FieldInfos.Add(fieldInfo);
        }

        public void AddFieldInfo<TTaskField>(TTaskField fieldEnum, ETaskFieldValueSource valueSource, string value)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = fieldEnum.ToString();
            fieldInfo.ValueSource = valueSource;
            fieldInfo.Value += value;
            FieldInfos.Add(fieldInfo);
        }

        public void AddTaskConnectPoint<TTaskField>(TTaskField fieldEnum, params int[] taskIds)
            where TTaskField : Enum
        {
            var list = SimplePool<List<int>>.Alloc();
            list.AddRange(taskIds);
            AddTaskConnectPoint(fieldEnum.ToString(), list);
            list.CollectToPool();
        }

        public void AddTaskConnectPoint(string fieldEnum, params int[] taskIds)
        {
            var list = SimplePool<List<int>>.Alloc();
            list.AddRange(taskIds);
            AddTaskConnectPoint(fieldEnum, list);
            list.CollectToPool();
        }

        public void AddTaskConnectPoint(string fieldEnum, List<int> taskIds)
        {
            TaskFieldInfo fieldInfo = new TaskFieldInfo();
            bool hasField = false;
            int fieldIndex = 0;
            for (int i = 0; i < FieldInfos.Count; i++)
            {
                if (FieldInfos[i].FieldName == fieldEnum)
                {
                    if (FieldInfos[i].ValueSource != ETaskFieldValueSource.Value)
                    {
                        throw new Exception($"Field {fieldEnum} already exists but its value source is not Value, cannot add task connect point");
                    }
                    fieldInfo = FieldInfos[i];
                    hasField = true;
                    fieldIndex = i;
                    break;
                }
            }

            if (hasField)
            {
                var sb = new StringBuilder();
                for (int i = 0; i < taskIds.Count; i++)
                {
                    sb.Append(taskIds[i].ToString());
                    sb.Append(TaskExportCrossVariable.ListElementSplit);
                }
                fieldInfo.Value += sb.ToString();
                FieldInfos[fieldIndex] = fieldInfo;
            }
            else
            {
                fieldInfo.FieldName = fieldEnum;
                fieldInfo.ValueSource = ETaskFieldValueSource.Value;
                var sb = new StringBuilder();
                for (int i = 0; i < taskIds.Count; i++)
                {
                    sb.Append(taskIds[i].ToString());
                    sb.Append(TaskExportCrossVariable.ListElementSplit);
                }
                fieldInfo.Value += sb.ToString();
                FieldInfos.Add(fieldInfo);
            }
        }

        public void AddFieldInfoFromBlackboard<TTaskField>(TTaskField taskFieldEnum, string key)
            where TTaskField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = taskFieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Blackboard;
            fieldInfo.Value = key;
            FieldInfos.Add(fieldInfo);
        }
        
        public void AddFieldInfoFromContext<TTaskField, TContextField>(TTaskField taskFieldEnum, TContextField contextFieldEnum)
            where TTaskField : Enum where TContextField : Enum
        {
            var fieldInfo = new TaskFieldInfo();
            fieldInfo.FieldName = taskFieldEnum.ToString();
            fieldInfo.ValueSource = ETaskFieldValueSource.Context;
            fieldInfo.Value = contextFieldEnum.ToString();
            FieldInfos.Add(fieldInfo);
        }

        public void AddTimelineInfo(float startTime, float duration, int referenceId)
        {
            var timelineInfo = new TaskTimelineItemInfo();
            timelineInfo.StartTime = startTime;
            timelineInfo.Duration = duration;
            timelineInfo.Id = referenceId;
            TimelineItemInfos.Add(timelineInfo);
        }

        public void AddEnterCondition(params int[] ids)
        {
            EnterConditionReferences.AddArray(ids);
        }

        public void AddCondition(params int[] ids)
        {
            ConditionReferences.AddArray(ids);
        }

        public void AddExitCondition(params int[] ids)
        {
            ExitConditionReferences.AddArray(ids);
        }
        #endregion
    }

    public class TaskGroupInfo
    {
        public int RootTaskId;
        public string BindingContextFullType;
        public Dictionary<int, TaskValueInfo> TaskInfos = new();

        public void SetRootTaskId(int taskId)
        {
            RootTaskId = taskId;
        }
    }
}
