using LightController.Color;
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
        private DmxChannel flatAddressMap;
        private IEnumerable<DmxChannel> addressMap;

        public DmxDevice(Config.Dmx.DmxDeviceProfile profile, Config.Dmx.DmxDeviceAddress address)
        {
            frame = new DmxFrame(profile.AddressMap, profile.DmxLength, address.StartAddress);
            flatAddressMap = profile.FlatAddressMap;
        }


        public DmxFrame GetFrame(ColorHSI target)
        {
            // Intensity
            double intensity = target.Intensity;
            if (Contains(DmxChannel.Intensity))
                target = new ColorHSI(target.Hue, target.Saturation, 1);

            ColorRGB rgb = (ColorRGB)target;

            frame.Reset();

            return frame;
        }

        /// <summary>
        /// Creates a new color channel from a color and a mask
        /// </summary>
        private byte RemoveColor(ColorRGB color, DmxChannel channel)
        {
            ColorRGB mask;
            switch (channel)
            {
                case DmxChannel.White:
                    mask = new ColorRGB(255, 255, 255);
                    break;
                default:
                    return 0;
            }

            double r = color.Red / mask.Red;
            double g = color.Green / mask.Green;
            double b = color.Blue / mask.Blue;
            double amount = Math.Min(Math.Min(r, g), b);
            byte value = (byte)(amount * 255);
            color.Red -= value;
            color.Green -= value;
            color.Blue -= value;
            return value;
        }

        private bool Contains(DmxChannel address)
        {
            return (flatAddressMap & address) != 0;
        }

    }
}
