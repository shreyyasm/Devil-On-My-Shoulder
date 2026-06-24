using System;
using UnityEditor;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationEventIcon
    {
        public Texture Menu => EditorGUIUtility.IconContent("_Menu").image;
        public Texture Settings => EditorGUIUtility.IconContent("_Popup@2x").image;

        public GUIContent Preview => new GUIContent("Preview", "Enable/disable scene preview mode.");
        
        public GUIContent GoToFirst => new GUIContent(
            EditorGUIUtility.IconContent("Animation.FirstKey").image,
            "Go to the beginning of the animation clip.");

        public GUIContent Prev => new GUIContent(
            EditorGUIUtility.IconContent("Animation.PrevKey").image,
            "Go to previous keyframe.");
        
        public GUIContent Play => new GUIContent(
            EditorGUIUtility.IconContent("Animation.Play").image,
            "Play the animation clip.");

        public GUIContent Next => new GUIContent(
            EditorGUIUtility.IconContent("Animation.NextKey").image,
            "Go to next keyframe.");

        public GUIContent GoToLast => new GUIContent(
            EditorGUIUtility.IconContent("Animation.LastKey").image,
            "Go to the end of the animation clip.");
        
        public GUIContent FindAnimation => new GUIContent(
            EditorGUIUtility.IconContent("DotFrame").image,
            "Locate animation clip.");
        
        public GUIContent NewColor => new GUIContent(
            EditorGUIUtility.IconContent("animationanimated").image,
            "Set to default.");
        
        public GUIContent RemoveColor => new GUIContent(
            EditorGUIUtility.IconContent("CrossIcon").image,
            "Remove Selected Color.");
        
        public GUIContent LockIcon => new GUIContent(
            EditorGUIUtility.IconContent("LockIcon").image,
            "Lock the window.");
        
        public GUIContent LockOnIcon => new GUIContent(
            EditorGUIUtility.IconContent("LockIcon-On").image,
            "Unlock the window.");
        
        public GUIContent ApplyChannelTemplate => new GUIContent(
            "Apply",
            "Apply current channel template.");
        
        public GUIContent SaveChannelTemplate => new GUIContent(
            "Save",
            "Save current channel template.");
        
        public GUIContent RemoveChannelTemplate => new GUIContent(
            "X",
            "Remove channel template.");

        public GUIContent ReadOnlyWarning => new GUIContent(Warn, "Duplicate the animation clip");
        
        public Texture AddChannel => EditorGUIUtility.IconContent("Animation.AddKeyframe").image;
        public Texture AddEvent => EditorGUIUtility.IconContent("Animation.AddEvent").image;
        public Texture EventMarker => EditorGUIUtility.IconContent("Animation.EventMarker").image;
        public Texture EventKey => EditorGUIUtility.IconContent("blendKey").image;
        public Texture Remove => EditorGUIUtility.IconContent("CrossIcon").image;
        public Texture Warn => EditorGUIUtility.IconContent("console.warnicon.sml").image;
    }
}