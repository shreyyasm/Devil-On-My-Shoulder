using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationEditorOptions
    {
        public bool showSeconds = false;
        public bool showSampleRate = true;
        public int samples = 30;
        public bool showAllClips = false;
        public AnimationEventTooltipState eventToolTip = AnimationEventTooltipState.ShowAlways;
        public bool showHoverInfo = true;
        public int animationSpeedMax = 2;
        public List<KColor> userColors = new List<KColor>();
        public bool useCurveAnimationSpeed = false;
        public KSort animationSortOrder = KSort.NameAscending;
        public bool applyRootMotion = true;
        
        // Window Options
        public bool autoApplyColor = false;
        
        // Experimantal
        private bool _enableExperimantalMode = false;
        
        public AnimationCurve animationSpeedCurve = AnimationCurve.Constant(0,1,1);

        public bool ExperimentMode
        {
            get => _enableExperimantalMode;
            set => _enableExperimantalMode = value;
        }
        
        public void Load(ref bool sSeconds, ref bool sSampleRate, ref int sSamples, ref bool sAllClips, ref AnimationEventTooltipState sToolTip, ref bool sShowHoverInfo, ref int sAnimationSpeed, ref List<KColor> sUserColors, ref bool sUseCurveAnimSpeed, ref KSort sAnimationSortOrder, ref bool sApplyRootMotion)
        {
            sSeconds = showSeconds;
            sSampleRate = showSampleRate;
            sSamples = samples;
            sAllClips = showAllClips;
            sToolTip = eventToolTip;
            sShowHoverInfo = showHoverInfo;
            sAnimationSpeed = animationSpeedMax;
            sUserColors = new List<KColor>();
            sUserColors = userColors;
            sUseCurveAnimSpeed = useCurveAnimationSpeed;
            sAnimationSortOrder = animationSortOrder;
            sApplyRootMotion = applyRootMotion;
        }
        
        public void Save(bool sSeconds, bool sSampleRate, int sSamples, bool sAllClips, AnimationEventTooltipState sToolTip, bool sShowHoverInfo, int sAnimationSpeed, List<KColor> sUserColors, bool sUseCurveAnimSpeed, KSort sAnimationSortOrder, bool sApplyRootMotion)
        {
            showSeconds = sSeconds;
            showSampleRate = sSampleRate;
            samples = sSamples;
            showAllClips = sAllClips;
            eventToolTip = sToolTip;
            showHoverInfo = sShowHoverInfo;
            animationSpeedMax = sAnimationSpeed;
            userColors = new List<KColor>();
            userColors = sUserColors;
            useCurveAnimationSpeed = sUseCurveAnimSpeed;
            animationSortOrder = sAnimationSortOrder;
            applyRootMotion = sApplyRootMotion;
        }

        public void LoadWindowOptions(ref bool sAutoApplyColor)
        {
            sAutoApplyColor = autoApplyColor;
        }

        public void SaveWindowOptions(bool sAutoApplyColor)
        {
            autoApplyColor = sAutoApplyColor;
        }
        
    }
}