using System;
using System.Collections.Generic;
using BbxCommon;

namespace BbxCommon
{
    public class TaskOnceRandomNumber : TaskOnceBase
    {
        public float MinValue;
        public float MaxValue;
        public string WriteToBlackboardKey;

        public enum EField
        {
            MinValue,
            MaxValue,
            WriteToBlackboardKey,
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.MinValue, MinValue, (fieldInfo, context) => { MinValue = ReadFloat(fieldInfo, context); });
            RegisterField(EField.MaxValue, MaxValue, (fieldInfo, context) => { MaxValue = ReadFloat(fieldInfo, context); });
            RegisterField(EField.WriteToBlackboardKey, WriteToBlackboardKey, (fieldInfo, context) => { WriteToBlackboardKey = ReadString(fieldInfo, context); });
        }

        protected override void OnTaskCollect()
        {
            MinValue = 0;
            MaxValue = 0;
            WriteToBlackboardKey = string.Empty;
        }

        protected override EOnceState OnExecute()
        {
            var randomNumber = UnityEngine.Random.Range(MinValue, MaxValue);
            TaskContext.SetBlackBoardDoubleValue(WriteToBlackboardKey, randomNumber);
            return EOnceState.Succeeded;
        }
    }
}