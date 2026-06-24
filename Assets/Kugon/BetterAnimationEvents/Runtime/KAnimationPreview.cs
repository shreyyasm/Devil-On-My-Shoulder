using System;
using UnityEditor;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationPreview
    {
        
        public bool IsPreview
        {
            get
            {
                return AnimationMode.InAnimationMode();
            }
            set
            {
                if (value == AnimationMode.InAnimationMode()) return;
        
                if (value) AnimationMode.StartAnimationMode();
                else AnimationMode.StopAnimationMode();
            }
        }
        
        public void SampleAnimationClip(GameObject root, KAnimationClipData clipData, int frameIndex)
        {
            if (!IsPreview || clipData == null) return;
            
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(root, clipData.Clip, frameIndex / clipData.FrameRate);
            AnimationMode.EndSampling();
        }
        
        public void SampleAnimationClip(GameObject root,KAnimationClipData clipData, ref float time)
        {
            if (!IsPreview || clipData == null) return;

            time %= clipData.Lenght;
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(root, clipData.Clip, time);
            AnimationMode.EndSampling();
        }
    }
}