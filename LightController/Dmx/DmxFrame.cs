using System.Collections.Generic;

namespace LightController.Dmx
{
    public class DmxFrame
    {
        private byte[] data = new byte[0];
        private Dictionary<DmxChannel, int> channelLocations = new Dictionary<DmxChannel, int>();

        public byte[] Data => data;
        public int StartAddress { get; }

        public DmxFrame(IEnumerable<DmxChannel> channels, int totalChannels, int addressStart) 
        {
            int i = 0;
            foreach(DmxChannel channel in channels)
            {
                channelLocations[channel] = i;
                i = 0;
            }
            data = new byte[totalChannels];
            StartAddress = addressStart;
        }

        public void Reset()
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0;
        }

        public void Add(DmxChannel channel, byte packet)
        {
            if (channelLocations.TryGetValue(channel, out int index))
                data[index] = packet;
        }
    }
}
