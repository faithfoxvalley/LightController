using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Dmx
{
    public class DmxDevice
    {
        private DmxFrame frame;
        private List<DmxChannel> addressMap;
        private bool hasIntensity;

        public DmxDevice(Config.Dmx.DmxDeviceProfile profile, Config.Dmx.DmxDeviceAddress address)
        {
            frame = new DmxFrame(profile.DmxLength, address.StartAddress);
            addressMap = profile.AddressMap.Where(x => x != null).OrderByDescending(x => x.MaskSize).ToList();
            hasIntensity = addressMap.Find(x => x.IsIntensity) != null;
        }


        public DmxFrame GetFrame(Colourful.RGBColor rgb, double intensity)
        {
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
