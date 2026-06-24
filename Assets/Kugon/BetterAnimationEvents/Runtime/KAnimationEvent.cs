using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationEvent // Event Keys
    {
        public string name;
        public int channelIndex = 0;
        public  KColor assignedColor = KColor.Default();
        
        public float time = 0.0f;
        public string eventFunctionName = "";
        public string eventString = "";
        public int eventInt = 0;
        public float eventFloat = 0.0f;
        public Object eventObject = (Object) null;
        
        public KAnimationEvent(float time, int channelIndex)
        {
            this.time = time;
            this.channelIndex = channelIndex;
        }

        public KAnimationEvent(AnimationEvent animationEvent)
        {
            eventFunctionName = animationEvent.functionName;
            eventFloat = animationEvent.floatParameter;
            eventInt = animationEvent.intParameter;
            eventString = animationEvent.stringParameter;
            eventObject = animationEvent.objectReferenceParameter;
            time = animationEvent.time;
        }
        
        public bool IsEqual(AnimationEvent animationEvent)
        {
            return
                Mathf.Approximately(time, animationEvent.time) &&
                eventFunctionName == animationEvent.functionName &&
                eventString == animationEvent.stringParameter &&
                eventInt == animationEvent.intParameter &&
                Mathf.Approximately(eventFloat, animationEvent.floatParameter) &&
                eventObject == animationEvent.objectReferenceParameter;
        }

        public KAnimationEvent Copy()
        {
            return new KAnimationEvent(time, channelIndex)
            {
                eventFunctionName = eventFunctionName,
                eventString = eventString,
                eventInt = eventInt,
                eventFloat = eventFloat,
                eventObject = eventObject,
                assignedColor = assignedColor
            };
        }
    }
}