using System;
using UnityEditor;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KUndoHelper
    {
        private string _prevName = "";

        private AnimationEventSave _container;

        public KUndoHelper(AnimationEventSave container)
        {
            _container = container;
        }
        
        public void RecordObject(string name, bool multiple = false)
        {
            if (_container == null) return;
            
            if (!multiple && _prevName == name) return;
            
            Undo.RecordObject(_container, name);
            _prevName = name;
        }
        
        public void SaveRecord()
        {
            if (_container == null) return;

            EditorUtility.SetDirty(_container);
            Undo.IncrementCurrentGroup();
            _prevName = "";
        }

        public void RecordAction(Action action)
        {
            RecordObject("Do Something");
            action.Invoke();
            SaveRecord();
        }
    }
}