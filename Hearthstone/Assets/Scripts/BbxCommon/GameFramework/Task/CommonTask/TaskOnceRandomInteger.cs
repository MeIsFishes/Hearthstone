using System;

namespace BbxCommon
{
    [TaskComment("Select an integer from an inclusive range and write it to a long Blackboard value.")]
    public sealed class TaskOnceRandomInteger : TaskOnceBase
    {
        [TaskComment("Inclusive lower bound. Bounds are swapped when this value is greater than MaxInclusive.")]
        public int MinInclusive;

        [TaskComment("Inclusive upper bound. Bounds are swapped when this value is less than MinInclusive.")]
        public int MaxInclusive;

        [TaskComment("Target long Blackboard key. The node fails when the key is empty.")]
        public string WriteToBlackboardKey;

        public enum EField
        {
            MinInclusive,
            MaxInclusive,
            WriteToBlackboardKey,
        }

        protected override void RegisterFields()
        {
            RegisterField(EField.MinInclusive, MinInclusive,
                (field, context) => MinInclusive = ReadInt(field, context));
            RegisterField(EField.MaxInclusive, MaxInclusive,
                (field, context) => MaxInclusive = ReadInt(field, context));
            RegisterField(EField.WriteToBlackboardKey, WriteToBlackboardKey,
                (field, context) => WriteToBlackboardKey = ReadString(field, context));
        }

        protected override EOnceState OnExecute()
        {
            if (string.IsNullOrWhiteSpace(WriteToBlackboardKey))
                return EOnceState.Failed;

            var minimum = Math.Min(MinInclusive, MaxInclusive);
            var maximum = Math.Max(MinInclusive, MaxInclusive);
            var range = (long)maximum - minimum + 1L;
            var offset = Math.Min((long)(UnityEngine.Random.value * range), range - 1L);
            TaskContext.SetBlackBoardLongValue(WriteToBlackboardKey, minimum + offset);
            return EOnceState.Succeeded;
        }

        protected override void OnTaskCollect()
        {
            MinInclusive = 0;
            MaxInclusive = 0;
            WriteToBlackboardKey = string.Empty;
        }
    }
}
