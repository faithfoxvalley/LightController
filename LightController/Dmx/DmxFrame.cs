using System;

namespace LightController.Dmx
{
    public class DmxFrame
    {
        private byte[] baseData; // Data before color/intensity values are added
        private byte[] mixData; // Old data to mix with new data
        private byte[] data; // Data to be sent
        private DateTime mixStart;
        private TimeSpan mixTime;

        public byte[] Data => data;
        public int StartAddress { get; }

        public DmxFrame(byte[] baseData, int addressStart) 
        {
            this.baseData = baseData;
            data = new byte[baseData.Length];
            mixData = new byte[baseData.Length];
            StartAddress = addressStart;
        }

        public void Reset()
        {
            for (int i = 0; i < baseData.Length; i++)
                data[i] = baseData[i];
        }

        public void Set(int index, double packet)
        {
            if(packet > 255)
                data[index] = 255;
            else
                data[index] = (byte)packet;
        }

        public void StartMix(double mixLength)
        {
            mixStart = DateTime.Now;
            mixTime = TimeSpan.FromSeconds(mixLength);
            mixData = data;
            data = new byte[mixData.Length];
            Reset();
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
