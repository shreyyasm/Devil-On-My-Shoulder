using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class SelectionHandler
    {
        public int selectedChannel { get; private set; }
        // private MethodInfo selectedFunction;
        public string selectedMethodID = "";
        public List<KAnimationEvent> SelectedEvents { get; private set; }

        private HashSet<KAnimationEvent> quickLookup;
        
        public bool HasChanged { get; set; }

        public SelectionHandler()
        {
            selectedChannel = 0;
            SelectedEvents = new List<KAnimationEvent>();
            quickLookup = new HashSet<KAnimationEvent>(2500);
        }

        public void Select(KAnimationEvent select, bool multipleSelection = false, bool auto = false)
        {
            if (auto)
            {
                if (SelectedEvents.Contains(select)) return;
            }
            
            if (multipleSelection)
            {
                if (SelectedEvents.Contains(select))
                {
                    SelectedEvents.Remove(select);
                    quickLookup.Remove(select);
                }
                else
                {
                    SelectedEvents.Add(select);
                    quickLookup.Add(select);
                }
                return;
            }

            SelectedEvents.Clear();
            SelectedEvents.Add(select);
            quickLookup.Clear();
            quickLookup.Add(select);
        }
        
        public bool HasSelected(KAnimationEvent compare)
        {
            return quickLookup.Contains(compare);
        }
        
        public void Unselect(KAnimationEvent unselect)
        {
            foreach (var kevt in SelectedEvents)
            {
                if (kevt == unselect)
                {
                    SelectedEvents.Remove(kevt);
                    quickLookup.Remove(kevt);
                    return;
                }
            }
        }

        public void VerifySelected()
        {
            for (int i = SelectedEvents.Count -1; i >= 0; i--)
            {
                if (SelectedEvents[i] == null)
                {
                    SelectedEvents.RemoveAt(i);
                    quickLookup.Clear();
                    quickLookup = SelectedEvents.ToHashSet();
                }
            }
        }

        public void UnselectAll()
        {
            SelectedEvents.Clear();
            quickLookup.Clear();
        }

        public void DeleteEvents()
        {
            SelectedEvents.Clear();
            quickLookup.Clear();
            HasChanged = true;
        }

        public void SelectChannel(int selectedChannelIndex)
        {
            selectedChannel = selectedChannelIndex;
        }
        
        public bool HasSelectedEvents => SelectedEvents.Count > 0;
        
        public (string, bool) FunctionValue
        {
            get => GetMultiString(e => e.eventFunctionName);
            set => SetMultiString(value.Item1, (e, v) => e.eventFunctionName = v);
        }

        public (string, bool) StringValue
        {
            get => GetMultiString(e => e.eventString);
            set => SetMultiString(value.Item1, (e, v) => e.eventString = v);
        }
        
        public (int, bool) IntValue
        {
            get => GetMultiInt(e => e.eventInt);
            set => SetMultiInt(value.Item1, (e, v) => e.eventInt = v);
        }
        
        public (float, bool) FloatValue
        {
            get => GetMultiFloat(e => e.eventFloat);
            set => SetMultiFloat(value.Item1, (e, v) => e.eventFloat = v);
        }
        public (Object, bool) ObjectValue
        {
            get => GetMultiObject(e => e.eventObject);
            set => SetMultiObject(value.Item1, (e, v) => e.eventObject = v);
        }
        
        private (string, bool) GetMultiString(Func<KAnimationEvent, string> getter)
        {
            if (!HasSelectedEvents) return ("", false);

            string first = getter(SelectedEvents[0]);

            for (int i = 1; i < SelectedEvents.Count; i++)
            {
                if (getter(SelectedEvents[i]) != first)
                    return ("—", true);
            }

            return (first, false);
        }

        private void SetMultiString(string value, Action<KAnimationEvent, string> setter)
        {
            if (!HasSelectedEvents || value == "—")
                return;

            foreach (var kevt in SelectedEvents)
            {
                setter(kevt, value);
            }
        }
        
        private (int, bool) GetMultiInt(Func<KAnimationEvent, int> getter)
        {
            if (!HasSelectedEvents) return (0, false);

            int first = getter(SelectedEvents[0]);

            for (int i = 1; i < SelectedEvents.Count; i++)
                if (!Mathf.Approximately(getter(SelectedEvents[i]), first))
                    return (0, true);

            return (first, false);
        }
        
        private void SetMultiInt(int value, Action<KAnimationEvent, int> setter)
        {
            if (!HasSelectedEvents) return;

            foreach (var e in SelectedEvents)
                setter(e, value);
        }
        
        private (float, bool) GetMultiFloat(Func<KAnimationEvent, float> getter)
        {
            if (!HasSelectedEvents) return (0f, false);

            float first = getter(SelectedEvents[0]);

            for (int i = 1; i < SelectedEvents.Count; i++)
                if (!Mathf.Approximately(getter(SelectedEvents[i]), first))
                    return (float.NaN, true);

            return (first, false);
        }
        
        private void SetMultiFloat(float value, Action<KAnimationEvent, float> setter)
        {
            if (!HasSelectedEvents || float.IsNaN(value)) return;

            foreach (var e in SelectedEvents)
                setter(e, value);
        }

        private (Object, bool) GetMultiObject(Func<KAnimationEvent, Object> getter)
        {
            if (!HasSelectedEvents) return (null, false);

            Object first = getter(SelectedEvents[0]);

            for (int i = 1; i < SelectedEvents.Count; i++)
            {
                if (getter(SelectedEvents[i]) != first)
                    return (null, true);
            }

            return (first, false);
        }

        private void SetMultiObject(Object value, Action<KAnimationEvent, Object> setter)
        {
            if (!HasSelectedEvents) return;

            foreach (var e in SelectedEvents)
                setter(e, value);
        }

        // public MethodInfo SelectedFunction
        // {
        //     get
        //     {
        //         return selectedFunction;
        //     }
        //     set
        //     {
        //         selectedFunction = value;
        //         if (value == null)
        //         {
        //             // FunctionValue = ("", false);
        //             selectedFunction = null;
        //             return;
        //         }
        //         FunctionValue = (selectedFunction.Name, false);
        //     }
        // }
    }
}