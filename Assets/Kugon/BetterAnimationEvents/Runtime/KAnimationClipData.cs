using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationClipData // Animation Clip Data
    {
        public AnimationClip Clip;
        public List<KAnimationChannel> Channels;
        public List<KAnimationEvent> Events;

        private AnimationEvent[] originalEvents;

        public DateTime CreatedTime;
        public DateTime LastUpdatedTime;

        public KAnimationClipData(AnimationClip clip)
        {
            Clip = clip;
            Channels = new List<KAnimationChannel>();
            Events = new List<KAnimationEvent>();
            AddChannel();
            CreatedTime = DateTime.Now;
            LastUpdatedTime = DateTime.Now;
        }

#if UNITY_6000_5_OR_NEWER
        public EntityId ID => GetSessionClipID(Clip);
#else
        public int ID => GetSessionClipID(Clip);
#endif
        public string Name => Clip.name;
        public float FrameRate => Clip.frameRate;
        public float Lenght => Clip.length;
        public int FrameCount => Mathf.RoundToInt(FrameRate * Lenght);
        
        public float TimeByFrame(int frameIndex) => frameIndex / FrameRate;
        public int FrameByTime(float time) => Mathf.RoundToInt(time * FrameRate);

        public bool IsValid => Clip != null;

        
        public KAnimationChannel AddChannel()
        {
            Channels.Add(new KAnimationChannel("-", Channels.Count));
            return Channels[^1];
        }

        public KAnimationEvent AddEvent(KAnimationEvent kEvent)
        {
            while (kEvent.channelIndex >= Channels.Count)
            {
                AddChannel();
            }

            bool shifDown = false;
            foreach (var kevt in Events)
            {
                if (kevt.time == kEvent.time && kevt.channelIndex == kEvent.channelIndex)
                {
                    shifDown = true;
                    break;
                }
            }

            if (shifDown)
            {
                foreach (var kevt in Events)
                {
                    if (kevt.channelIndex >= kEvent.channelIndex)
                    {
                        kevt.channelIndex++;

                        if (kevt.channelIndex >= Channels.Count)
                        {
                            AddChannel();
                        }
                    }
                }
            }

            Events.Add(kEvent);
            SortEvents();
            return kEvent;
        }
        
        public KAnimationEvent AddEvent(int channelIndex, float time)
        {
            KAnimationEvent customEvent = new KAnimationEvent(time, channelIndex);
            return AddEvent(customEvent);
        }

        public void SortEvents()
        {
            Events.Sort(Sorting);
        }

        private int Sorting(KAnimationEvent a, KAnimationEvent b)
        {
            if (a.channelIndex < b.channelIndex) return -1;
            else if (a.channelIndex > b.channelIndex) return 1;

            if (a.time < b.time) return -1;
            else if (a.time > b.time) return 1;

            return 0;
        }

        public void RemoveEvent(KAnimationEvent eventToRemove)
        {
            foreach (var evt in Events)
            {
                if (evt == eventToRemove)
                {
                    Events.Remove(evt);
                    return;
                }
            }
        }

        private List<KAnimationEvent> GetAllEvents()
        {
            return Events;
        }

        // public void LoadFromSave(AnimationClipSaveData saveFile)
        // {
        //     var loaded = saveFile.Load();
        //     Clip = loaded.Clip;
        //     Events = loaded.Events;
        //     Channels = loaded.Channels;
        //     if (Channels.Count == 0) AddChannel();
        // }

        public void Reset()
        {
            Channels = new List<KAnimationChannel>();
            AddChannel();
            Events = new List<KAnimationEvent>();
            foreach (var orgEvt in AnimationUtility.GetAnimationEvents(Clip))
            {
                KAnimationEvent kEvent = new KAnimationEvent(orgEvt);
                AddEvent(kEvent);
            }
        }

        public bool HasSaved()
        {
            var allCurrentEvents = GetAllEvents();

            // TODO Optimze Animation Get AnimatinEvents Creates too much Garbage Allocation
            originalEvents = AnimationUtility.GetAnimationEvents(Clip);
            if (allCurrentEvents.Count != originalEvents.Length) return false;

            foreach (var current in allCurrentEvents)
            {
                bool matchFound = false;

                foreach (var original in originalEvents)
                {
                    if (current.IsEqual(original))
                    {
                        matchFound = true;
                        break;
                    }
                }

                if (!matchFound)
                    return false; // at least one mismatch
            }

            return true;
        }

        public void RefreshKAnimationClipData(KMethodHandler methodHandler)
        {
            // foreach (var kevt in Events)
            // {
            //     kevt.RefreshKAnimationEvent(methodHandler);
            // }
        }

        public void DuplicateAnimationClip()
        {
            if (IsReadOnly())
            {
                string path = AssetDatabase.GetAssetPath(Clip);
                string newPath = AssetDatabase.GenerateUniqueAssetPath(
                    Path.Combine(Path.GetDirectoryName(path), Clip.name + ".anim"));

                var originalClip = Clip;
                Clip = Object.Instantiate(Clip);
                Clip.name = originalClip.name;
                AssetDatabase.CreateAsset(Clip, newPath);

                foreach (var guid in AssetDatabase.FindAssets("t:AnimatorController"))
                {
                    var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guid));

                    if (controller == null) continue;

                    bool modified = false;
                    foreach (var layer in controller.layers)
                    foreach (var state in layer.stateMachine.states)
                    {
                        if (state.state.motion == originalClip)
                        {
                            state.state.motion = Clip;
                            modified = true;
                        }
                        else if (state.state.motion is BlendTree blendTree)
                        {
                            if (ReplaceInBlendTree(blendTree, originalClip, Clip))
                                modified = true;
                        }
                    }

                    if (modified)
                        EditorUtility.SetDirty(controller);
                }
            }
        }
        
        private bool ReplaceInBlendTree(BlendTree blendTree, AnimationClip originalClip, AnimationClip newClip)
        {
            bool modified = false;
            var children = blendTree.children;

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].motion == originalClip)
                {
                    children[i].motion = newClip;
                    modified = true;
                }
                else if (children[i].motion is BlendTree nestedBlendTree)
                {
                    if (ReplaceInBlendTree(nestedBlendTree, originalClip, newClip))
                        modified = true;
                }
            }

            if (modified)
                blendTree.children = children;

            return modified;
        }
        
        public bool SaveToAnimation()
        {
            List<AnimationEvent> foundEvents = new List<AnimationEvent>();
        
            foreach (var kevt in GetAllEvents())
            {
                AnimationEvent evt = new AnimationEvent
                {
                    functionName = kevt.eventFunctionName,
                    stringParameter = kevt.eventString,
                    intParameter = kevt.eventInt,
                    floatParameter = kevt.eventFloat,
                    objectReferenceParameter = kevt.eventObject,
                    time = kevt.time
                };
        
                foundEvents.Add(evt);
            }
        
            AnimationUtility.SetAnimationEvents(Clip, foundEvents.ToArray());
            return true;
        }

        public void UpdateTime()
        {
            LastUpdatedTime = DateTime.Now;
        }
        
        public bool IsReadOnly()
        {
            return IsClipReadOnly(Clip);
        }
        
        public static bool IsClipReadOnly(AnimationClip clip)
        {
            string path = AssetDatabase.GetAssetPath(clip);

            if (AssetImporter.GetAtPath(path) is ModelImporter) return true;
            if (!AssetDatabase.IsMainAsset(clip) && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(clip))) return true;
            if (!AssetDatabase.IsNativeAsset(clip)) return true;

            return false;
        }

        public static string GetPersistentClipID(AnimationClip clip)
        {
            return AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(clip));
        }

#if UNITY_6000_5_OR_NEWER
        public static EntityId GetSessionClipID(AnimationClip clip)
        {
            return clip.GetEntityId();
        }
#else
        public static int GetSessionClipID(AnimationClip clip)
        {
            return clip.GetInstanceID();
        }
#endif

        
        
        // public List<KAnimationChannel> CopyChannel()
        // {
        //     List<KAnimationChannel> copiedChannels = new List<KAnimationChannel>();
        //
        //     foreach (var channal in channels)
        //     {
        //         var cChannel = new KAnimationChannel(channal.name, channal.index);
        //         
        //         foreach (var evt in channal.events)
        //             cChannel.events.Add(evt.Copy());
        //         
        //         copiedChannels.Add(cChannel);
        //     }
        //
        //     return copiedChannels;
        // }
    }
}