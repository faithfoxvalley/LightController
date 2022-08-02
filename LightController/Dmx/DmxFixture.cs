using Colourful;
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
        private int fixtureId;

        public DmxFixture(Config.Dmx.DmxDeviceProfile profile, Config.Dmx.DmxDeviceAddress address, int fixtureId)
        {
            frame = new DmxFrame(profile.DmxLength, address.StartAddress);
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
                    this.input = input;
                    break;
                }
            }
        }

        public DmxFrame GetFrame()
        {
            double intensity = 1; // TODO
            RGBColor rgb = input.GetColor();

            frame.Reset();

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
