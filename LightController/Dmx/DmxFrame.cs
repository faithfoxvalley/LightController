﻿using LightController.Color;
using System;
using System.Collections.Generic;

namespace LightController.Dmx
{
    public class DmxFrame
    {
        private byte[] baseData; // Data before color/intensity values are added
        private byte[] mixData; // Old data to mix with new data
        private double[] rawData; // Data before being clamped to byte range
        private double maxData; // Maximum value in data
        private byte[] data; // Data to be sent
        private DateTime mixStart;
        private TimeSpan mixTime;

        public byte[] Data => data;
        public int StartAddress { get; }

        public System.Windows.Media.Color PreviewColor { get; private set; }
        public double PreviewIntensity { get; private set; }

        public DmxFrame(byte[] baseData, int addressStart) 
        {
            this.baseData = baseData;
            data = new byte[baseData.Length];
            mixData = new byte[baseData.Length];
            rawData = new double[baseData.Length];
            StartAddress = addressStart;
            Reset();
        }

        public void SetPreviewData(ColorRGB color, double intensity)
        {
            PreviewColor = new System.Windows.Media.Color()
            {
                R = color.Red,
                G = color.Green,
                B = color.Blue,
            };
            PreviewIntensity = intensity;
        }

        public void Reset()
        {
            for (int i = 0; i < baseData.Length; i++)
            {
                data[i] = baseData[i];
                rawData[i] = baseData[i];
            }
            maxData = 0;
        }

        public void Set(int index, double packet)
        {
            rawData[index] = packet;
            if (packet > maxData)
                maxData = packet;
            if(packet > 254.5)
                data[index] = 255;
            else
                data[index] = (byte)packet;
        }

        public void StartMix(double mixLength, double mixDelay)
        {
            mixStart = DateTime.Now + TimeSpan.FromSeconds(mixDelay);
            mixTime = TimeSpan.FromSeconds(mixLength);
            mixData = data;
            data = new byte[mixData.Length];
            Reset();
        }

        public void Mix()
        {
            if (mixStart.Ticks <= 0 || mixTime.Ticks == 0)
                return;

            double percentNewData = 0;
            TimeSpan length = DateTime.Now - mixStart;
            if (length.Ticks > 0) // Negative value indicates mixing hasnt started yet
            {
                if (length > mixTime)
                    return;

                percentNewData = length.Ticks / (double)mixTime.Ticks;
            }

            for(int i = 0; i < mixData.Length && i < data.Length; i++)
            {
                double newData = data[i] * percentNewData;
                double oldData = mixData[i] * (1 - percentNewData);
                double newValue = newData + oldData;
                if (newValue > 254.5)
                    data[i] = 255;
                else
                    data[i] = (byte)newValue;
            }
        }

        /// <summary>
        /// Normalizes the dmx data to be between 0 and 255 if the maximum is larger than 255
        /// </summary>
        /// <param name="indices">The indices to normalize</param>
        public void Clamp(IEnumerable<int> indices)
        {
            if (maxData <= 255)
                return;

            double factor = 255 / maxData;
            foreach(int i in indices)
            {
                double packet = rawData[i] * factor;
                if (packet > 254.5)
                    data[i] = 255;
                else
                    data[i] = (byte)packet;
            }
        }
    }
}
