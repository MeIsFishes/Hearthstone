using System;
using System.Collections.Generic;
using BbxCommon;

namespace BbxCommon
{
    public class TaskNodeWaitForTime : TaskBase
    {
        public float Time;
        private float m_ElapsedTime;

        public enum EField
        {
            Time,
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.Time, Time, (fieldInfo, context) => { Time = ReadFloat(fieldInfo, context); });
        }

        protected override void OnTaskCollect()
        {
            Time = 0;
            m_ElapsedTime = 0;
        }

        protected override void OnEnter()
        {
            m_ElapsedTime = 0f;
        }

        protected override ETaskRunState OnUpdate(float deltaTime)
        {
            m_ElapsedTime += deltaTime;
            if (m_ElapsedTime < Time)
            {
                return ETaskRunState.Running;
            }
            return ETaskRunState.Succeeded;
        }

        protected override void OnExit()
        {

        }
    }
}
