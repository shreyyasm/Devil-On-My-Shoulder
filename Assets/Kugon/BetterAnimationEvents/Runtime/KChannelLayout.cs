using System;
using System.Collections.Generic;

namespace Kugon.BetterAnimationEvents
{
    [Serializable]
    public class KChannelLayout
    {
        public string layoutName;
        public List<string> channelNames;
        
        public KChannelLayout(string layoutName, List<string> channelNames)
        {
            this.layoutName = layoutName;
            this.channelNames = channelNames;
        }
    }
}