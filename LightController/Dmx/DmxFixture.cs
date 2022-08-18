using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightController.Dmx
{
    public class DmxFixture
    {
        private DmxFrame frame;
        private DmxChannel intensityChannel;
        private double? maxLumens;
        private List<DmxChannel> colorChannels = new List<DmxChannel>();
        private Config.Input.InputBase input;
        private bool disabled;
        private bool newInput;
        private object inputLock = new object();
        private int fixtureId;
        private double mixLength;
        private string detailString;

        public int FixtureId => fixtureId;

        public DmxFixture(Config.Dmx.DmxDeviceProfile profile, int dmxStartAddress, int fixtureId)
        {
            detailString = $"{fixtureId} - {profile.Name} - {dmxStartAddress}-{dmxStartAddress + profile.DmxLength - 1}";

            // Construct the default frame
            byte[] data = new byte[profile.DmxLength];
            
            foreach(DmxChannel channel in profile.AddressMap)
            {
                if (channel != null)
                {
                    if (channel.Constant.HasValue)
                        data[channel.Index] = channel.Constant.Value;
                    else if (channel.IsIntensity)
                        intensityChannel = channel;
                    else if(channel.IsColor)
                        colorChannels.Add(channel);
                }
            }

            if (colorChannels.All(x => x.Lumens.HasValue))
                maxLumens = colorChannels.Max(x => x.Lumens.Value);

            frame = new DmxFrame(data, dmxStartAddress);
            colorChannels = colorChannels.OrderByDescending(x => x.MaskSize).ToList();

            this.fixtureId = fixtureId;
        }

        public void TurnOff()
        {
            lock(inputLock)
            {
                disabled = true;
                input = null;
                newInput = true;
            }
        }

        public void SetInput(IEnumerable<Config.Input.InputBase> inputs, double mixLength)
        {
            if (double.IsNaN(mixLength) || double.IsInfinity(mixLength) || mixLength < 0)
                mixLength = 0;

            foreach (var input in inputs)
            {
                if (input.FixtureIds.Contains(fixtureId))
                {
                    lock(inputLock)
                    {
                        if(!disabled)
                        {
                            this.mixLength = mixLength;
                            this.input = input;
                            newInput = true;
                        }
                    }
                    return;
                }
            }

            lock(inputLock)
            {
                if (!disabled)
                {
                    this.mixLength = mixLength;
                    this.input = null;
                    newInput = true;
                }
            }

        }

        public DmxFrame GetFrame()
        {

            ColorHSV hsv;
            double intensity;

            lock (inputLock)
            {
                if (disabled)
                {
                    frame.Reset();
                    return frame;
                }

                if (newInput)
                {
                    frame.StartMix(mixLength);
                    newInput = false;
                }

                frame.Reset();

                if (input == null)
                {
                    frame.Mix();
                    return frame;
                }

                hsv = input.GetColor(fixtureId);
                intensity = input.GetIntensity(fixtureId, hsv);
            }

            // Make a copy with maximum intensity
            ColorRGB rgb = (ColorRGB)new ColorHSV(hsv.Hue, hsv.Saturation, 1);

            if(intensityChannel != null)
                frame.Set(intensityChannel.Index, intensity * 255);

            foreach (DmxChannel channel in colorChannels)
            {
                double value = channel.GetColorValue(ref rgb) * 255;
                if (intensityChannel == null)
                    value *= intensity;
                if (maxLumens.HasValue)
                    value *= maxLumens.Value / channel.Lumens.Value;
                frame.Set(channel.Index, value);
            }

            frame.Clamp(colorChannels.Select(x => x.Index));
            frame.Mix();

            return frame;
        }

        public DmxFrame GetOffFrame()
        {
            frame.Reset();
            return frame;
        }

        public override string ToString()
        {
            return detailString;
        }
    }
}
