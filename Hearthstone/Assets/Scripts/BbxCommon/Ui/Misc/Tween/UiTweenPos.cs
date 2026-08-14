using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BbxCommon.Ui
{
    public class UiTweenPos : UiTweenBase<Vector3>
    {
        public enum EPosType
        {
            RelativeLocalPos,
            AbsoluteLocalPos,
        }

        [FoldoutGroup("Play Tween")]
        public EPosType PosType = EPosType.AbsoluteLocalPos;

        protected override void ApplyTween(Component component, float evaluate)
        {
            if (component == null)
                return;

            var setter = component as UiTransformSetter;
            if (setter != null)
            {
                switch (PosType)
                {
                    case EPosType.RelativeLocalPos:
                        setter.PosWrapper.AddLocalPositionRequest(setter.transform.localPosition + MinValue + (MaxValue - MinValue) * evaluate, UiTransformSetter.EPosPriority.Tween);
                        break;
                    case EPosType.AbsoluteLocalPos:
                        setter.PosWrapper.AddLocalPositionRequest(MinValue + (MaxValue - MinValue) * evaluate, UiTransformSetter.EPosPriority.Tween);
                        break;
                }
            }
            else
            {
                switch (PosType)
                {
                    case EPosType.RelativeLocalPos:
                        component.transform.localPosition = component.transform.localPosition + MinValue + (MaxValue - MinValue) * evaluate;
                        break;
                    case EPosType.AbsoluteLocalPos:
                        component.transform.localPosition = MinValue + (MaxValue - MinValue) * evaluate;
                        break;
                }
            }
        }

        protected override ESearchTarget GetSearchTarget()
        {
            return ESearchTarget.Single;
        }

        protected override void GetSearchType(List<Type> types)
        {
            types.Add(typeof(UiTransformSetter));
            types.Add(typeof(Transform));
        }
    }
}
