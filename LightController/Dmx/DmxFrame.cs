using System.Collections.Generic;

namespace LightController.Dmx
{
    public class DmxFrame
    {
        private double[] rawData = new double[0];
        private byte[] data = new byte[0];

        public byte[] Data => data;
        public int StartAddress { get; }

        public DmxFrame(int totalChannels, int addressStart) 
        {
            data = new byte[totalChannels];
            rawData = new double[totalChannels];
            StartAddress = addressStart;
        }

        public void Reset()
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0;
            for (int i = 0; i < rawData.Length; i++)
                rawData[i] = 0;
        }

        public void Set(int index, double packet)
        {
            rawData[index] = packet;

            if(packet > 255)
                data[index] = 255;
            else
                data[index] = (byte)packet;
        }

        public double Get(int index)
        {
            return rawData[index];
        }
    }
}
