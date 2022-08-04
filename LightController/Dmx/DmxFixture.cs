using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Dmx
{
    public class DmxFixture
    {
        private DmxFrame frame;
        private List<DmxChannel> addressMap;
        private bool hasIntensity;
        private Config.Input.InputBase input;
        private object inputLock = new object();
        private int fixtureId;

        public DmxFixture(Config.Dmx.DmxDeviceProfile profile, int dmxStartAddress, int fixtureId)
        {
            frame = new DmxFrame(profile.DmxLength, dmxStartAddress);
            addressMap = profile.AddressMap.Where(x => x != null).OrderByDescending(x => x.MaskSize).ToList();
            hasIntensity = addressMap.Find(x => x.IsIntensity) != null;
            this.fixtureId = fixtureId;
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
            double hueIntensity = Math.Max(Math.Max(rgb.Red, rgb.Green), rgb.Blue);
            if (hueIntensity > 0)
                hueIntensity = 255d / hueIntensity;
            else
                hueIntensity = 1;
            rgb = new ColorRGB(
                (byte)(rgb.Red / hueIntensity),
                (byte)(rgb.Green / hueIntensity),
                (byte)(rgb.Blue / hueIntensity));


            foreach (DmxChannel channel in addressMap)
            {
                double channelIntensity = 1;
                if(!hasIntensity || channel.IsIntensity)
                    channelIntensity = intensity;

                frame.Set(channel.Index, channel.GetValue(ref rgb, channelIntensity));
            }

            return frame;
        }

    }
}
