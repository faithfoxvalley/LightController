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
        private List<DmxChannel> colorChannels = new List<DmxChannel>();
        private Config.Input.InputBase input;
        private bool disabled;
        private bool newInput;
        private object inputLock = new object();
        private int fixtureId;
        private double mixLength;
        private string detailString;

        public DmxFixture(Config.Dmx.DmxDeviceProfile profile, int dmxStartAddress, int fixtureId, double mixLength)
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
            frame = new DmxFrame(data, dmxStartAddress);
            colorChannels = colorChannels.OrderByDescending(x => x.MaskSize).ToList();

            this.fixtureId = fixtureId;
            this.mixLength = mixLength;
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

        public void SetInput(IEnumerable<Config.Input.InputBase> inputs)
        {
            foreach(var input in inputs)
            {
                if (input.FixtureIds.Contains(fixtureId))
                {
                    lock(inputLock)
                    {
                        if(!disabled)
                        {
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
                    this.input = null;
                    newInput = true;
                }
            }

        }

        public DmxFrame GetFrame()
        {
            frame.Reset();

            ColorHSV hsv;
            double intensity;

            lock (inputLock)
            {
                if (disabled)
                    return frame;

                if (newInput)
                {
                    frame.StartMix(mixLength);
                    newInput = false;
                }

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
                frame.Set(channel.Index, value);
            }

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
