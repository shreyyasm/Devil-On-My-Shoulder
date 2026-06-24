using System;
using UnityEngine;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationEventInputs
    {
        public bool Copy(bool use = false) => KeyPress(EventType.KeyDown, KeyCode.C, use);
        public bool Paste(bool use = false) => KeyPress(EventType.KeyDown, KeyCode.V, use);
        public bool SpaceDown(bool use = false) => KeyPress(EventType.KeyDown, KeyCode.Space, use);
        public bool DeleteDown(bool use = false) => KeyPress(EventType.KeyDown, KeyCode.Delete, use);
        public bool EscapeDown(bool use = false) => KeyPress(EventType.KeyDown, KeyCode.Escape, use);
        public bool EscapeUp(bool use = false) => KeyPress(EventType.KeyUp, KeyCode.Escape, use);
        public bool FDown(bool use = false) => KeyPress(EventType.KeyDown, KeyCode.F, use);
        public bool Shift() => Event.current.keyCode == KeyCode.LeftShift;

        public bool MouseLeft(EventType eventType, bool use = false) => MousePress(eventType, 0, use);
        public bool MouseRight(EventType eventType, bool use = false) => MousePress(eventType, 1, use);
        public bool MouseMiddle(EventType eventType, bool use = false) => MousePress(eventType, 2, use);

        private bool KeyPress(EventType type, KeyCode keyCode, bool use = false)
        {
            Event key = Event.current;
            if(key.type == type && key.keyCode == keyCode)
            {
                if(use) key.Use();
                return true;
            }
            return false;
        }

        private bool MousePress(EventType type, int button, bool use = false)
        {
            Event key = Event.current;
            if (key.rawType == type && key.button == button)
            {
                if(use) key.Use();
                return true;
            }
            return false;
        }
    }
}