using System;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KAnimationChannel // Event Channels
    {
        public string name;
        public int index;
    
        public KAnimationChannel(string name, int index)
        {
            this.name = name;
            this.index = index;
        }
    }
}