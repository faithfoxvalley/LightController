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

            ColorRGBW rgbw = (ColorRGBW)target;

            // Get byte values;
            byte white = 0;
            byte red, green, blue;
            if (Contains(DmxChannel.White))
            {
                white = rgbw.White;
                red = rgbw.Red;
                green = rgbw.Green;
                blue = rgbw.Blue;
            }
            else
            {
                ColorRGB rgb = (ColorRGB)target;
                red = rgb.Red;
                green = rgb.Green;
                blue = rgb.Blue;
            }

            // Convert RGB and create other special types of channels
            byte amber = 0;
            if (Contains(DmxChannel.Amber))
                amber = RemoveColor(rgbw, new ColorRGB());

            byte indigo = 0;
            if (Contains(DmxChannel.Indigo))
                indigo = RemoveColor(rgbw, new ColorRGB());

            byte lime = 0;
            if (Contains(DmxChannel.Lime))
                lime = RemoveColor(rgbw, new ColorRGB());

            frame.Reset();

            foreach (DmxChannel color in AddressMap)
            {
                byte packet;
                switch (color)
                {
                    case DmxChannel.Intensity:
                        packet = (byte)(intensity * 255);
                        break;
                    case DmxChannel.White:
                        packet = Math.Min(Math.Min(rgbw.Red, rgbw.Green), rgbw.Blue);
                        break;
                    case DmxChannel.Amber:
                        packet = amber;
                        break;
                    case DmxChannel.Indigo:
                        packet = indigo;
                        break;
                    case DmxChannel.Lime:
                        packet = lime;
                        break;
                    case DmxChannel.Red:
                        packet = rgbw.Red;
                        break;
                    case DmxChannel.Green:
                        packet = rgbw.Green;
                        break;
                    case DmxChannel.Blue:
                        packet = rgbw.Blue;
                        break;
                    default:
                        continue;
                }
                frame.Add(packet);
            }

            return frame;
        }

        /// <summary>
        /// Creates a new color channel from a color and a mask
        /// </summary>
        private byte RemoveColor(ColorRGBW color, ColorRGB mask)
        {
            return 0;
        }

        private bool Contains(DmxChannel address)
        {
            return (flatAddressMap & address) != 0;
        }

    }
}
