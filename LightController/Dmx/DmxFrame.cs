using System;

namespace LightController.Dmx
{
    public class DmxFrame
    {

        private double[] rawData = new double[0];
        private byte[] data = new byte[0];
        private byte[] mixData = new byte[0];
        private DateTime mixStart;
        private TimeSpan mixTime;

        public byte[] Data => data;
        public int StartAddress { get; }

        public DmxFrame(int totalChannels, int addressStart) 
        {
            data = new byte[totalChannels];
            mixData = new byte[totalChannels];
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

        public void StartMix(double mixLength)
        {
            mixStart = DateTime.Now;
            mixTime = TimeSpan.FromSeconds(mixLength);
            mixData = data;
            data = new byte[mixData.Length];
        }

        public void Mix()
        {
            if (mixStart.Ticks <= 0 || mixTime.Ticks == 0)
                return;

            TimeSpan length = DateTime.Now - mixStart;
            if (length > mixTime)
                return;

            double percentNewData = length.Ticks / (double)mixTime.Ticks;

            for(int i = 0; i < mixData.Length && i < data.Length; i++)
            {
                double newData = data[i] * percentNewData;
                double oldData = mixData[i] * (1 - percentNewData);
                double newValue = newData + oldData;
                if (newValue > 255)
                    data[i] = 255;
                else
                    data[i] = (byte)newValue;
            }
        }
    }
}
