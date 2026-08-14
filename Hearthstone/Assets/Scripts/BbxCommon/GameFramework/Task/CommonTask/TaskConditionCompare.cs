using System;
using System.Collections.Generic;
using BbxCommon;

namespace BbxCommon
{
    public enum ECompare
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
    }

    public class TaskConditionCompare : TaskConditionBase
    {
        public float LeftValue;
        public ECompare CompareType;
        public float RightValue;

        public enum EField
        {
            LeftValue,
            CompareType,
            RightValue,
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.LeftValue, LeftValue, (fieldInfo, context) => { LeftValue = ReadFloat(fieldInfo, context); });
            RegisterField(EField.CompareType, CompareType, (fieldInfo, context) => { CompareType = ReadEnum<ECompare>(fieldInfo, context); });
            RegisterField(EField.RightValue, RightValue, (fieldInfo, context) => { RightValue = ReadFloat(fieldInfo, context); });
        }

        protected override void OnConditionCollect()
        {
            LeftValue = 0;
            CompareType = ECompare.Equal;
            RightValue = 0;
        }

        protected override EConditionState OnConditionUpdate(float deltaTime)
        {
            bool result = CompareType switch
            {
                ECompare.Equal => LeftValue == RightValue,
                ECompare.NotEqual => LeftValue != RightValue,
                ECompare.Greater => LeftValue > RightValue,
                ECompare.GreaterOrEqual => LeftValue >= RightValue,
                ECompare.Less => LeftValue < RightValue,
                ECompare.LessOrEqual => LeftValue <= RightValue,
            };
            return result ? EConditionState.Succeeded : EConditionState.Failed;
        }
    }
}