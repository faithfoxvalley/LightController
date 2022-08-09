using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightController.Dmx
{
    public class DmxFixture
    {
        private DmxFrame frame;
        private List<DmxChannel> addressMap;
        private List<int> colorChannels;
        private bool hasIntensity;
        private Config.Input.InputBase input;
        private object inputLock = new object();
        private int fixtureId;

        public DmxFixture(Config.Dmx.DmxDeviceProfile profile, int dmxStartAddress, int fixtureId)
        {
            frame = new DmxFrame(profile.DmxLength, dmxStartAddress);
            addressMap = profile.AddressMap.Where(x => x != null).OrderByDescending(x => x.MaskSize).ToList();
            colorChannels = addressMap.Where(x => x.IsColor).Select(x => x.Index).ToList();
            hasIntensity = addressMap.Find(x => x.IsIntensity) != null;
            this.fixtureId = fixtureId;

            CompensateForLumens();
        }

        /// <summary>
        /// Compensate for some bulbs having different physical intensities.
        /// </summary>
        private void CompensateForLumens()
        {
            bool? hasLumens = null;
            double maxLumens = double.NegativeInfinity;
            foreach(DmxChannel channel in addressMap)
            {
                if(channel.IsColor)
                {
                    double lumens = channel.Lumens;
                    bool lumensExists = !double.IsNaN(lumens);
                    if (hasLumens.HasValue)
                    {
                        if(lumensExists != hasLumens.Value)
                            throw new Exception("Invalid mixing of dmx channel definitions with and without lumens.");
                    }
                    else
                    {
                        hasLumens = lumensExists;
                    }

                    if(lumensExists && lumens > maxLumens)
                        maxLumens = lumens;
                }
            }

            if(hasLumens.HasValue && hasLumens.Value)
            {
                foreach (DmxChannel channel in addressMap)
                {
                    if (channel.IsColor)
                        channel.ApplyIntensityMultiplier(maxLumens / channel.Lumens);
                }

            }
        }

        public void SetInput(IEnumerable<Config.Input.InputBase> inputs)
        {
            foreach(var input in inputs)
            {
                if (input.FixtureIds.Contains(fixtureId))
                {
                    lock(inputLock)
                    {
                        this.input = input;
                    }
                    return;
                }
            }

            lock(inputLock)
            {
                this.input = null;
            }
        }

        public DmxFrame GetFrame()
        {
            frame.Reset();

            ColorRGB rgb;
            double intensity;

            lock (inputLock)
            {
                if (input == null)
                    return frame;

                rgb = input.GetColor(fixtureId);
                intensity = input.GetIntensity(fixtureId, rgb);
            }

            // Make a copy with maximum intensity
            double hueIntensity = rgb.Max();
            if (hueIntensity > 0)
                hueIntensity = 255d / hueIntensity;
            else
                hueIntensity = 1;
            rgb = new ColorRGB(
                (byte)(rgb.Red * hueIntensity),
                (byte)(rgb.Green * hueIntensity),
                (byte)(rgb.Blue * hueIntensity));


            double maxValue = double.NegativeInfinity;
            foreach (DmxChannel channel in addressMap)
            {
                if(channel.IsIntensity)
                {
                    frame.Set(channel.Index, intensity * 255);
                }
                else if (channel.Constant.HasValue)
                {
                    frame.Set(channel.Index, channel.Constant.Value);
                }
                else if(channel.IsColor)
                {
                    // This value could be more than 255 if lumen compensation is on,
                    //   and in that case it must be normalized later.
                    double value = channel.GetColorValue(ref rgb) * 255;
                    if (!hasIntensity)
                        value *= intensity;
                    if(value > maxValue)
                        maxValue = value;
                    frame.Set(channel.Index, value);
                }
            }

            // If the values are too high, normalize the value back down to 255
            if (maxValue > 255)
            {
                maxValue = 255 / maxValue;
                foreach (int colorIndex in colorChannels)
                    frame.Set(colorIndex, frame.Get(colorIndex) * maxValue);
            }

            return frame;
        }

        public DmxFrame GetOffFrame()
        {
            frame.Reset();
            return frame;
        }
    }
}
