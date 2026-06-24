using System;
using System.Collections.Generic;
using System.Linq;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KClipBoard
    {
        public float time;
        public int channelIndex;
        public List<KAnimationEvent> clippedEvents;

        public bool HasCopy() => clippedEvents != null && clippedEvents.Count > 0;
        public void CopyEvents(List<KAnimationEvent> eventsToCopy)
        {
            if (eventsToCopy.Count == 0)
            {
                clippedEvents = new List<KAnimationEvent>();
                return;
            }

            clippedEvents = new List<KAnimationEvent>();
            clippedEvents = eventsToCopy.Select(e => e.Copy()).ToList();

            time = clippedEvents[0].time;
            channelIndex = clippedEvents[0].channelIndex;
            foreach (var kevt in clippedEvents)
            {
                if (kevt.time < time) time = kevt.time;
                if (kevt.channelIndex < channelIndex) channelIndex = kevt.channelIndex;
            }
        }
    }
}