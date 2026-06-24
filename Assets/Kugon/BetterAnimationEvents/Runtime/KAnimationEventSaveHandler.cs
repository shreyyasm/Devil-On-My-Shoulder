using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationEventSaveHandler
    {
        private const string SubfolderName = "Save Files";
        
        public AnimationEventSave SaveFile { get; private set; }
        private Dictionary<AnimationClip, KAnimationClipData> _dict;
        
        public Dictionary<AnimationClip, KAnimationClipData> Dict
        {
            get
            {
                if (_dict == null)
                    RebuildDictionary();
                return _dict;
            }
        }

        public void RebuildDictionary()
        {
            SaveFile.savedClips.RemoveAll(c => c == null || !c.IsValid);

            _dict = SaveFile.savedClips
                .GroupBy(c => c.Clip)
                .Select(g => g.First())
                .ToDictionary(c => c.Clip, c => c);

            SaveFile.savedClips = _dict.Values.ToList();

            SaveFileDirty();
        }
        public void LoadSaveFile()
        {
            SaveFile = LoadOrCreateInScriptFolder<AnimationEventSave>();
        }
        
        public T LoadOrCreateInScriptFolder<T>(string fileName = null) where T : ScriptableObject
        {
#if UNITY_EDITOR
            // Try to find the save file folder
            string[] scriptGUIDs = AssetDatabase.FindAssets("AnimationEventSaveHandler t:Script");
            if (scriptGUIDs.Length == 0)
            {
                Debug.LogError("Could not locate AnimationEventSaveHandler script.");
                return null;
            }

            string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGUIDs[0]);
            string baseFolder = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            string targetFolder = $"{baseFolder}/{SubfolderName}";

            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
                AssetDatabase.Refresh();
            }

            if (string.IsNullOrEmpty(fileName))
                fileName = typeof(T).Name;

            string assetPath = $"{targetFolder}/{fileName}.asset";

            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return asset;
#else
        return null;
#endif
        }

        public void Save(AnimationClip clip)
        {
            if (SaveFile == null)
                LoadSaveFile();
            
            if (clip == null) return;

            if (Dict.ContainsKey(clip))
            {
                // Debug.LogWarning($"{clip.name} + Has not saved due to because an identical clip already exists.");
                return;
            }
            
            var clipData = new KAnimationClipData(clip);
            clipData.Reset();

            SaveFile.savedClips.Add(clipData);
            _dict.Add(clip, clipData);

            SaveFileDirty();
        }

        public KAnimationClipData Load(AnimationClip clip)
        {
            if (clip == null) return null;
            
            if (SaveFile == null)
                LoadSaveFile();

            if (Dict.TryGetValue(clip, out var clipData))
                return clipData;

            // Create new
            var newData = new KAnimationClipData(clip);
            newData.Reset();
            SaveFile.savedClips.Add(newData);
            _dict.Add(clip, newData);

            SaveFileDirty();
            return newData;
        }

        public List<KChannelLayout> LoadChannelLayouts()
        {
            if (SaveFile == null) return new List<KChannelLayout>();

            return SaveFile.ChannelLayouts;
        }
        
        public void SaveFileDirty()
        {
            if (SaveFile == null) return;
            EditorUtility.SetDirty(SaveFile);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}