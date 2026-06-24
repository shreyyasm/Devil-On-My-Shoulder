using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    [CreateAssetMenu(fileName = "BetterAnimationEventsSave", menuName = "Animation/New Better Animation Events Save")]
    public class AnimationEventSave : ScriptableObject
    {
        public KAnimationEditorOptions EditorOptions = new KAnimationEditorOptions();
        // TODO use dictionary
        public List<KChannelLayout> ChannelLayouts = new List<KChannelLayout>();
        public AnimationClip undoSelected = null;
        public KMethodHandler.KMethodInfo undoMethod;
        public List<KAnimationClipData> savedClips = new List<KAnimationClipData>();

        public Dictionary<AnimationClip, KAnimationClipData> GetSave =>   savedClips
            .ToDictionary(e => e.Clip, e => e);
    }
}