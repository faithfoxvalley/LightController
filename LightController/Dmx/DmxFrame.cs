using System.Collections.Generic;

namespace LightController.Dmx
{
    public class DmxFrame
    {
        private byte[] data = new byte[0];

        public byte[] Data => data;
        public int StartAddress { get; }

        public DmxFrame(int totalChannels, int addressStart) 
        {
            data = new byte[totalChannels];
            StartAddress = addressStart;
        }

        public void Reset()
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0;
        }

        public void Set(int index, byte packet)
        {
            data[index] = packet;
        }
    }
}
