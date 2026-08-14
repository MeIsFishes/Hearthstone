using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace BbxCommon.Ui
{
    public class UiTweenScale : UiTweenBase<Vector3>
    {
        public enum EScaleType
        {
            RelativeScale,
            AbsoluteScale,
        }

        [FoldoutGroup("Play Tween")]
        public EScaleType ScaleType;

        protected override void ApplyTween(Component component, float evaluate)
        {
            if (component == null)
                return;

            var setter = component as UiTransformSetter;
            if (setter != null)
            {
                switch (ScaleType)
                {
                    case EScaleType.RelativeScale:
                        setter.ScaleWrapper.AddScaleRequest(Vector3.Scale(setter.transform.localScale, MinValue + (MaxValue - MinValue) * evaluate), UiTransformSetter.EScalePriority.Tween);
                        break;
                    case EScaleType.AbsoluteScale:
                        setter.ScaleWrapper.AddScaleRequest(MinValue + (MaxValue - MinValue) * evaluate, UiTransformSetter.EScalePriority.Tween);
                        break;
                }
            }
            else
            {
                switch (ScaleType)
                {
                    case EScaleType.RelativeScale:
                        component.transform.localScale = Vector3.Scale(component.transform.localScale, MinValue + (MaxValue - MinValue) * evaluate);
                        break;
                    case EScaleType.AbsoluteScale:
                        component.transform.localScale = MinValue + (MaxValue - MinValue) * evaluate;
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
