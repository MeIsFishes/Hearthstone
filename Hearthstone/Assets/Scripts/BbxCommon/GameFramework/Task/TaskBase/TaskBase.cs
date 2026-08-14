using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using BbxCommon.Internal;

namespace BbxCommon
{
    public enum ETaskRunState
    {
        None,
        Running,
        Succeeded,
        Failed,
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
	public class TaskCommentAttribute : Attribute
	{
		public string Comment;
		public TaskCommentAttribute(string comment)
		{
			Comment = comment;
		}
	}

    public abstract class TaskBase : PooledObject
    {
        #region Lifecycle
        public ETaskRunState State => m_State;
        public bool Finished => m_State == ETaskRunState.Succeeded || m_State == ETaskRunState.Failed;
        public TaskContextBase TaskContext;
        public Action OnFinished;
        /// <summary>Task resource key from <see cref="TaskApi.CreateTask"/> / <see cref="TaskApi.RunTask(string, TaskContextBase)"/>; set only on the root node.</summary>
        public string SourceTaskKey { get; internal set; }
        internal TaskBridgeValueInfo TaskValueInfo;

        private int m_TypeId;
        private ETaskRunState m_State;
        private bool m_PreCollectPrepared;
        private List<TaskBase> m_OwnedTaskInstances;

        // EnterCondition: Check once when task executes Enter().
        // Condition: Check every frame. If failed, the task will be stopped and return failed.
        // ExitCondition: Check every frame. If succeeded, the task will be stopped and return succeeded.
        private TaskConnectPoint m_EnterCondition = new();
        private TaskConnectPoint m_Conditions = new();
        private TaskConnectPoint m_ExitConditions = new();

        public TaskBase()
        {
            m_TypeId = TaskDeserialiser.GetTaskTypeId(GetType());
            RegisterFields();
        }

        public void Run()
        {
            m_State = ETaskRunState.None;
            TaskManager.Instance.RunTask(this);
        }

        internal void RunInCurrentTaskTick()
        {
            m_State = ETaskRunState.None;
            TaskManager.Instance.RunTaskInCurrentTick(this);
        }

        public void AddEnterCondition(TaskConditionBase condition)
        {
            m_EnterCondition.Tasks.Add(condition);
        }

        public void AddCondition(TaskConditionBase condition)
        {
            m_Conditions.Tasks.Add(condition);
        }

        public void AddExitCondition(TaskConditionBase condition)
        {
            m_ExitConditions.Tasks.Add(condition);
        }

        internal void Enter()
        {
            m_State = ETaskRunState.Running;
            for (int i = 0; i < m_EnterCondition.Tasks.Count; i++)
            {
                var condition = m_EnterCondition.Tasks[i];
                condition.Enter();
                var state = condition.Update(0);
                condition.Exit();
                if (state == ETaskRunState.Failed)
                {
                    m_State = ETaskRunState.Failed;
                    return;
                }
            }
            for (int i = 0; i < m_Conditions.Tasks.Count; i++)
            {
                m_Conditions.Tasks[i].Enter();
            }
            for (int i = 0; i < TaskValueInfo.BlackboardFieldInfos.Count; i++)
            {
                ReadFieldInfo(TaskValueInfo.BlackboardFieldInfos[i].FieldEnumValue, TaskValueInfo.BlackboardFieldInfos[i], TaskContext);
            }
            OnEnter();
        }

        public bool CanEnter()
        {
            for (int i = 0; i < m_EnterCondition.Tasks.Count; i++)
            {
                var condition = m_EnterCondition.Tasks[i];
                condition.Enter();
                var state = condition.Update(0);
                condition.Exit();
                if (state == ETaskRunState.Failed)
                {
                    m_State = ETaskRunState.Failed;
                    return false;
                }
            }
            return true;
        }

        internal ETaskRunState Update(float deltaTime)
        {
            if (m_State == ETaskRunState.Succeeded || m_State == ETaskRunState.Failed)
                return m_State;
            for (int i = 0; i < m_Conditions.Tasks.Count; i++)
            {
                if (m_Conditions.Tasks[i].Update(deltaTime) == ETaskRunState.Failed)
                {
                    m_State = ETaskRunState.Failed;
                    return ETaskRunState.Failed;
                }
            }
            for (int i = 0; i < m_ExitConditions.Tasks.Count; i++)
            {
                if (m_ExitConditions.Tasks[i].Update(deltaTime) == ETaskRunState.Succeeded)
                {
                    m_State = ETaskRunState.Succeeded;
                    return ETaskRunState.Succeeded;
                }
            }
            var state = OnUpdate(deltaTime);
            // Conditions are polled by their owner and must remain re-evaluable while the owner runs.
            if (this is not TaskConditionBase &&
                (state == ETaskRunState.Succeeded || state == ETaskRunState.Failed))
                m_State = state;
            return state;
        }

        internal void Exit()
        {
            for (int i = 0; i < m_Conditions.Tasks.Count; i++)
            {
                m_Conditions.Tasks[i].Exit();
            }
            for (int i = 0; i < m_ExitConditions.Tasks.Count; i++)
            {
                m_ExitConditions.Tasks[i].Exit();
            }
            if (m_State == ETaskRunState.Failed)
            {
                OnNodeFailed();
            }
            else
            {
                m_State = ETaskRunState.Succeeded;
                OnNodeSucceeded();
            }
            OnExit();
        }

        internal void OnNodeSucceeded() { OnSucceeded(); }
        internal void OnNodeFailed() { OnFailed(); }

        protected virtual void OnEnter() { }
        protected virtual ETaskRunState OnUpdate(float deltaTime) { return ETaskRunState.Succeeded; }
        protected virtual void OnExit() { }
        protected virtual void OnSucceeded() { }
        protected virtual void OnFailed() { }

        protected sealed override void OnAllocate()
        {
            m_PreCollectPrepared = false;
            OnTaskAllocate();
        }

        protected sealed override void OnCollect()
        {
            PrepareForOwnedTaskInstancesCollect();
            try
            {
                if (m_OwnedTaskInstances != null)
                {
                    // The owned graph is a flat list whose order is not a parent-before-child
                    // contract. Prepare every node before any node loses runtime state to
                    // pooling, so nested compensation is order-independent.
                    for (var i = 0; i < m_OwnedTaskInstances.Count; i++)
                    {
                        var task = m_OwnedTaskInstances[i];
                        if (task != null && !ReferenceEquals(task, this))
                            task.PrepareForOwnedTaskInstancesCollect();
                    }
                    for (var i = 0; i < m_OwnedTaskInstances.Count; i++)
                    {
                        var task = m_OwnedTaskInstances[i];
                        if (task != null && !ReferenceEquals(task, this))
                            task.CollectToPool();
                    }
                    m_OwnedTaskInstances.CollectToPool();
                    m_OwnedTaskInstances = null;
                }
                m_EnterCondition.Reset();
                m_Conditions.Reset();
                m_ExitConditions.Reset();
                TaskValueInfo = null;
                TaskContext = null;
                OnFinished = null;
                SourceTaskKey = null;
                OnTaskCollect();
            }
            finally
            {
                m_PreCollectPrepared = false;
            }
        }

        protected virtual void OnTaskAllocate() { }
        protected virtual void OnBeforeOwnedTaskInstancesCollect() { }
        protected virtual void OnTaskCollect() { }

        private void PrepareForOwnedTaskInstancesCollect()
        {
            if (m_PreCollectPrepared)
                return;
            m_PreCollectPrepared = true;
            OnBeforeOwnedTaskInstancesCollect();
        }

        internal void OwnTaskInstances(List<TaskBase> taskInstances)
        {
            m_OwnedTaskInstances = taskInstances;
        }
        #endregion

        #region Read Field Info

        #region Common
        public void ReadFieldInfo(int fieldEnum, TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            m_RegisteredFieldList[fieldEnum].FieldCallback(fieldInfo, context);
        }

        protected T ReadValue<T>(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            var res = default(T);
            if (fieldInfo.Inited == false)
            {
                if (fieldInfo.ValueSource == ETaskFieldValueSource.Context)
                {
                    fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                }
            }
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    DebugApi.LogWarning("Task Value: Use other functions which meets the type required instead!");
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = (T)context.GetConstValue(fieldInfo.ConstValue.IntValue).ObjectValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (T)context.GetBlackBoardObjectValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            if (res == null)
            {
                var typeT = typeof(T);
                if (typeT.IsValueType && Nullable.GetUnderlyingType(typeT) == null)
                    res = (T)Activator.CreateInstance(typeT);
            }
            return res;
        }
        #endregion

        #region Base Type
        protected bool ReadBool(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            bool res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.BoolValue = false;
                            break;
                        }
                        if (bool.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: bool, content: " + str);
                        }
                        fieldInfo.ConstValue.BoolValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.BoolValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).BoolValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = context.GetBlackBoardLongValue(fieldInfo.ConstValue.StringValue) > 0;
                    break;
            }
            return res;
        }

        protected short ReadShort(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            short res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.ShortValue = 0;
                            break;
                        }
                        if (short.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: short, content: " + str);
                        }
                        fieldInfo.ConstValue.ShortValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.ShortValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).ShortValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (short)context.GetBlackBoardLongValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected ushort ReadUshort(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            ushort res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.UshortValue = 0;
                            break;
                        }
                        if (ushort.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: ushort, content: " + str);
                        }
                        fieldInfo.ConstValue.UshortValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.UshortValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).UshortValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (ushort)context.GetBlackBoardLongValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected int ReadInt(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            int res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.IntValue = 0;
                            break;
                        }
                        if (int.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: int, content: " + str);
                        }
                        fieldInfo.ConstValue.IntValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.IntValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).IntValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (int)context.GetBlackBoardLongValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected uint ReadUint(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            uint res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.UintValue = 0;
                            break;
                        }
                        if (uint.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: uint, content: " + str);
                        }
                        fieldInfo.ConstValue.UintValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.UintValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).UintValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (uint)context.GetBlackBoardLongValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected long ReadLong(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            long res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.LongValue = 0;
                            break;
                        }
                        if (long.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: long, content: " + str);
                        }
                        fieldInfo.ConstValue.LongValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.LongValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).LongValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = context.GetBlackBoardLongValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected ulong ReadUlong(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            ulong res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.UlongValue = 0;
                            break;
                        }
                        if (ulong.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: ulong, content: " + str);
                        }
                        fieldInfo.ConstValue.UlongValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.UlongValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).UlongValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (ulong)context.GetBlackBoardLongValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected float ReadFloat(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            float res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.FloatValue = 0;
                            break;
                        }
                        if (float.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: float, content: " + str);
                        }
                        fieldInfo.ConstValue.FloatValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.FloatValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).FloatValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (float)context.GetBlackBoardDoubleValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected double ReadDouble(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            double res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.DoubleValue = 0;
                            break;
                        }
                        if (double.TryParse(str, out res) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: double, content: " + str);
                        }
                        fieldInfo.ConstValue.DoubleValue = res;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.DoubleValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).DoubleValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = context.GetBlackBoardDoubleValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected T ReadEnum<T>(TaskBridgeFieldInfo fieldInfo, TaskContextBase context) where T : Enum
        {
            T res = default;
            // init
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.ObjectValue = default(T);
                            break;
                        }
                        if (Enum.TryParse(typeof(T), str, out var obj) == false)
                        {
                            DebugApi.LogError("Task value parse failed! Task: " + this.GetType().Name + ", fieldEnumValue: " + fieldInfo.FieldEnumValue +
                                ", required type: " + typeof(T).Name + ", content: " + str);
                        }
                        fieldInfo.ConstValue.ObjectValue = (T)obj;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = (T)fieldInfo.ConstValue.ObjectValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = (T)context.GetConstValue(fieldInfo.ConstValue.IntValue).ObjectValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    if (string.IsNullOrEmpty(fieldInfo.ConstValue.StringValue))
                        return default;
                    res = context.GetBlackBoardValue<T>(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }

        protected string ReadString(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            // init
            string res = default;
            if (fieldInfo.Inited == false)
            {
                var str = fieldInfo.ConstValue.StringValue;
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (str.IsNullOrEmpty())
                            fieldInfo.ConstValue.StringValue = null;
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    res = fieldInfo.ConstValue.StringValue;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return default;
                    res = context.GetConstValue(fieldInfo.ConstValue.IntValue).StringValue;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    res = (string)context.GetBlackBoardObjectValue(fieldInfo.ConstValue.StringValue);
                    break;
            }
            return res;
        }
        #endregion

        #region Special Type
        protected void ReadList<T>(TaskBridgeFieldInfo fieldInfo, TaskContextBase context, List<T> res, bool isConnectPoint = false)
        {
            if (res == null)
                return;
            res.Clear();
            // init
            if (fieldInfo.Inited == false)
            {
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        if (fieldInfo.ConstValue.StringValue.IsNullOrEmpty())
                        {
                            fieldInfo.ConstValue.ObjectValue = new List<T>();
                            return;
                        }
                        var elements = fieldInfo.ConstValue.StringValue.Split(TaskExportCrossVariable.ListElementSplit, StringSplitOptions.RemoveEmptyEntries);
                        if (res is List<bool>)
                        {
                            var boolList = new List<bool>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (bool.TryParse(elements[i], out var val))
                                {
                                    boolList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = boolList;
                        }
                        else if (res is List<char>)
                        {
                            var charList = new List<char>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (char.TryParse(elements[i], out var val))
                                {
                                    charList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = charList;
                        }
                        else if (res is List<sbyte>)
                        {
                            var sbyteList = new List<sbyte>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (sbyte.TryParse(elements[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                                {
                                    sbyteList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = sbyteList;
                        }
                        else if (res is List<byte>)
                        {
                            var byteList = new List<byte>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (byte.TryParse(elements[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                                {
                                    byteList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = byteList;
                        }
                        else if (res is List<short>)
                        {
                            var shortList = new List<short>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (short.TryParse(elements[i], out var val))
                                {
                                    shortList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = shortList;
                        }
                        else if (res is List<ushort>)
                        {
                            var ushortList = new List<ushort>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (ushort.TryParse(elements[i], out var val))
                                {
                                    ushortList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = ushortList;
                        }
                        else if (res is List<int>)
                        {
                            var intList = new List<int>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (int.TryParse(elements[i], out var val))
                                {
                                    intList.Add(val);
                                }
                            }
                            if (isConnectPoint)
                            {
                                for (int i = 0; i < intList.Count; i++)
                                {
                                    intList[i] = context.BindingTaskGroupInfo.ReorderedIndexDic[intList[i]];
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = intList;
                        }
                        else if (res is List<uint>)
                        {
                            var uintList = new List<uint>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (uint.TryParse(elements[i], out var val))
                                {
                                    uintList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = uintList;
                        }
                        else if (res is List<long>)
                        {
                            var longList = new List<long>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (long.TryParse(elements[i], out var val))
                                {
                                    longList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = longList;
                        }
                        else if (res is List<ulong>)
                        {
                            var ulongList = new List<ulong>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (ulong.TryParse(elements[i], out var val))
                                {
                                    ulongList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = ulongList;
                        }
                        else if (res is List<float>)
                        {
                            var floatList = new List<float>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (float.TryParse(elements[i], out var val))
                                {
                                    floatList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = floatList;
                        }
                        else if (res is List<double>)
                        {
                            var doubleList = new List<double>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (double.TryParse(elements[i], out var val))
                                {
                                    doubleList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = doubleList;
                        }
                        else if (res is List<decimal>)
                        {
                            var decimalList = new List<decimal>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (decimal.TryParse(elements[i], NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                                {
                                    decimalList.Add(val);
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = decimalList;
                        }
                        else if (res is List<string>)
                        {
                            var stringList = new List<string>();
                            for (int i = 0; i < elements.Length; i++)
                            {
                                stringList.Add(elements[i]);
                            }
                            fieldInfo.ConstValue.ObjectValue = stringList;
                        }
                        else if (typeof(T).IsEnum)
                        {
                            var enumList = new List<T>();
                            var addMethod = enumList.GetType().GetMethod("Add");
                            for (int i = 0; i < elements.Length; i++)
                            {
                                if (Enum.TryParse(typeof(T), elements[i], out var val))
                                {
                                    addMethod.Invoke(enumList, new object[] { val });
                                }
                            }
                            fieldInfo.ConstValue.ObjectValue = enumList;
                        }
                        else
                        {
                            fieldInfo.ConstValue.ObjectValue = new List<T>();
                            DebugApi.LogWarning("Task Value: Parse custom types for collections are currently not supported!");
                        }
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            // get value
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    var valueList = fieldInfo.ConstValue.ObjectValue as List<T>;
                    if (valueList != null)
                        res.AddList(valueList);
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return;
                    var contextList = context.GetConstValue(fieldInfo.ConstValue.IntValue).ObjectValue as List<T>;
                    if (contextList != null)
                        res.AddList(contextList);
                    break;
                case ETaskFieldValueSource.Blackboard:
                    var blackboardList = context.GetBlackBoardObjectValue(fieldInfo.ConstValue.StringValue) as List<T>;
                    if (blackboardList != null)
                        res.AddList(blackboardList);
                    break;
            }
            return;
        }

        protected void ReadDictionary<TKey, TValue>(TaskBridgeFieldInfo fieldInfo, TaskContextBase context, Dictionary<TKey, TValue> res)
        {
            if (res == null)
                return;
            res.Clear();
            if (fieldInfo.Inited == false)
            {
                switch (fieldInfo.ValueSource)
                {
                    case ETaskFieldValueSource.Value:
                        fieldInfo.ConstValue.ObjectValue = fieldInfo.ConstValue.StringValue.IsNullOrEmpty()
                            ? new Dictionary<TKey, TValue>()
                            : JsonApi.DeserializeFromString<Dictionary<TKey, TValue>>(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Context:
                        fieldInfo.ConstValue.IntValue = context.GetStrIndex(fieldInfo.ConstValue.StringValue);
                        break;
                    case ETaskFieldValueSource.Blackboard:
                        break;
                }
            }
            Dictionary<TKey, TValue> source = null;
            switch (fieldInfo.ValueSource)
            {
                case ETaskFieldValueSource.Value:
                    source = fieldInfo.ConstValue.ObjectValue as Dictionary<TKey, TValue>;
                    break;
                case ETaskFieldValueSource.Context:
                    if (fieldInfo.ConstValue.IntValue == TaskContextBase.UnboundContextFieldIndex)
                        return;
                    source = context.GetConstValue(fieldInfo.ConstValue.IntValue).ObjectValue as Dictionary<TKey, TValue>;
                    break;
                case ETaskFieldValueSource.Blackboard:
                    source = context.GetBlackBoardObjectValue(fieldInfo.ConstValue.StringValue) as Dictionary<TKey, TValue>;
                    break;
            }
            if (source == null)
                return;
            foreach (var pair in source)
                res[pair.Key] = pair.Value;
        }
        #endregion

        #region Task Connect Point
        /// <summary>
        /// For task instances should be deserialized first, it can't read refrences during deserialization. We cache those
        /// connect point and then initialize them after deserialization is done.
        /// </summary>
        private List<TaskConnectPoint> m_CachedConnectPoint = new();

        /// <summary>
        /// Read task refrences for child nodes. Most frequently used in low-level drive nodes, such as Sequence, Selector in
        /// behavior tree.
        /// </summary>
        protected TaskConnectPoint ReadConnectPoint(TaskBridgeFieldInfo fieldInfo, TaskContextBase context)
        {
            var res = new TaskConnectPoint();
            ReadList(fieldInfo, context, res.TaskRefrenceIds, true);
            m_CachedConnectPoint.Add(res);
            return res;
        }

        internal void InitConnectPoint(List<TaskBase> taskList)
        {
            if (m_CachedConnectPoint.Count == 0)
                return;
            for (int i = 0; i < m_CachedConnectPoint.Count; i++)
            {
                var connectPoint = m_CachedConnectPoint[i];
                for (int j = 0; j < connectPoint.TaskRefrenceIds.Count; j++)
                {
                    connectPoint.Tasks.Add(taskList[connectPoint.TaskRefrenceIds[j]]);
                }
                connectPoint.TaskRefrenceIds.Clear();
            }
            m_CachedConnectPoint.Clear();
        }
        #endregion

        #endregion

        #region Register Field Info
        internal class RegisteredField
        {
            internal string Name;
            internal TaskExportTypeInfo TypeInfo;
            internal Action<TaskBridgeFieldInfo, TaskContextBase> FieldCallback;
        }

        private List<RegisteredField> m_RegisteredFieldList = new();

        internal TaskExportInfo GenerateExportInfo()
        {
            RegisterFields();
            var res = new TaskExportInfo();
            res.TaskTypeName = this.GetType().Name;
            res.TaskFullTypeName = this.GetType().FullName;
            // tags
            var attributes = this.GetType().GetCustomAttributes(true);
            bool overriden = false;
            foreach (var attribute in attributes)
            {
                if (attribute is TaskTagAttribute tagAttribute)
                {
                    res.Tags.AddRange(tagAttribute.Tags);
                    overriden = tagAttribute.SetTag == TaskTagAttribute.ESetTag.Override;
                }
            }
            if (overriden == false)
            {
                if (this is TaskDurationBase)
                {
                    res.Tags.Add(TaskExportCrossVariable.TaskTagAction);
                    res.Tags.Add(TaskExportCrossVariable.TaskTagDuration);
                }
                else if (this is TaskOnceBase)
                {
                    res.Tags.Add(TaskExportCrossVariable.TaskTagAction);
                    res.Tags.Add(TaskExportCrossVariable.TaskTagOnce);
                }
                else if (this is TaskConditionBase)
                {
                    res.Tags.Add(TaskExportCrossVariable.TaskTagCondition);
                }
                else // hasn't been derrived
                {
                    res.Tags.Add(TaskExportCrossVariable.TaskTagAction);
                    res.Tags.Add(TaskExportCrossVariable.TaskTagNormal);
                }
            }
            var taskType = this.GetType();
            var classCommentAttrs = taskType.GetCustomAttributes(typeof(TaskCommentAttribute), true);
            if (classCommentAttrs.Length > 0)
                res.Comment = ((TaskCommentAttribute)classCommentAttrs[0]).Comment;
            // fields
            var eFieldType = taskType.GetNestedType("EField");
            var declaredOnlyField = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            foreach (var field in m_RegisteredFieldList)
            {
                var exportFieldInfo = new TaskExportFieldInfo();
                exportFieldInfo.FieldName = field.Name;
                exportFieldInfo.TypeInfo = field.TypeInfo;
                string comment = null;
                if (eFieldType != null)
                {
                    var memberInfos = eFieldType.GetMember(field.Name);
                    if (memberInfos.Length > 0)
                    {
                        var commentAttrs = memberInfos[0].GetCustomAttributes(typeof(TaskCommentAttribute), false);
                        if (commentAttrs.Length > 0)
                            comment = ((TaskCommentAttribute)commentAttrs[0]).Comment;
                    }
                }
                if (comment == null)
                {
                    for (var t = taskType; t != null && t != typeof(TaskBase); t = t.BaseType)
                    {
                        var fi = t.GetField(field.Name, declaredOnlyField);
                        if (fi == null)
                            continue;
                        var attrs = fi.GetCustomAttributes(typeof(TaskCommentAttribute), false);
                        if (attrs.Length > 0)
                        {
                            comment = ((TaskCommentAttribute)attrs[0]).Comment;
                            break;
                        }
                    }
                }
                exportFieldInfo.Comment = comment;
                res.FieldInfos.Add(exportFieldInfo);
            }
            return res;
        }

        protected abstract void RegisterFields();

        protected void RegisterField<TEnum, TObj>(TEnum fieldEnum, TObj obj, Action<TaskBridgeFieldInfo, TaskContextBase> fieldCallback) where TEnum : Enum
        {
            var fieldEnumString = fieldEnum.ToString();
            var fieldEnumValue = TaskDeserialiser.GetTaskFieldEnum(m_TypeId, fieldEnumString);
            if (m_RegisteredFieldList.Count < fieldEnumValue + 1)
                m_RegisteredFieldList.ModifyCount(fieldEnumValue + 1);
            if (m_RegisteredFieldList[fieldEnumValue] == null)
                m_RegisteredFieldList[fieldEnumValue] = new();
            var field = m_RegisteredFieldList[fieldEnumValue];
            field.Name = fieldEnumString;
            field.TypeInfo = TaskApi.GenerateTaskTypeInfo(typeof(TObj), obj);
            field.FieldCallback = fieldCallback;
        }
        #endregion
    }

    #region Task Extension
    public static class TaskExtension
    {
        public static TaskValueInfo CreateTaskValueInfo<T>(this TaskGroupInfo groupInfo, int id)
        {
            var info = new TaskValueInfo();
            info.FullTypeName = typeof(T).FullName;
            groupInfo.TaskInfos[id] = info;
            return info;
        }

        public static TaskValueInfo CreateTaskTimelineValueInfo(this TaskGroupInfo groupInfo, int id, float duration)
        {
            var info = groupInfo.CreateTaskValueInfo<TaskTimeline>(id);
            info.AddFieldInfo(TaskTimeline.EField.Duration, duration);
            return info;
        }
    }
    #endregion
}
