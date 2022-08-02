using LightController.Color;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Dmx
{
    public class DmxChannel
    {
        private ColorRGB mask;
        private byte? constantValue;
        private string stringValue;

        public int Index { get; private set; }
        public bool IsIntensity { get; private set; } = false;
        public double MaskSize => IsIntensity ? double.PositiveInfinity : mask.Red + mask.Green + mask.Blue;

        public DmxChannel(string stringValue, int index)
        {
            this.stringValue = stringValue;
            Index = index;
        }

        public static DmxChannel Parse(string value, int index)
        {
            if (value == null)
                return null;

            value = value.Trim().ToLowerInvariant();
            if(string.IsNullOrWhiteSpace(value))
                return null;

            DmxChannel result = new DmxChannel(value, index);

            if (value[0] == '#')
            {
                var color = ColorTranslator.FromHtml(value);
                if (color.IsEmpty)
                    return null;
                result.mask = ColorRGB.FromColor(color);
                return result;
            }

            ColorRGB mask;
            switch (value)
            {
                case "red":
                    mask = new ColorRGB(255, 0, 0);
                    break;
                case "green":
                    mask = new ColorRGB(0, 255, 0);
                    break;
                case "blue":
                    mask = new ColorRGB(0, 0, 255);
                    break;
                case "white":
                    mask = new ColorRGB(255, 255, 255);
                    break;
                case "amber":
                    mask = new ColorRGB(255, 191, 0);
                    break;
                case "intensity":
                    result.IsIntensity = true;
                    return result;
                default:
                    if(byte.TryParse(value, out byte b))
                    {
                        result.constantValue = b;
                        return result;
                    }
                    return null;
            }
            result.mask = mask;
            return result;
        }

        public byte GetValue(ref ColorRGB color, double intensity)
        {
            if (constantValue.HasValue)
                return constantValue.Value;

            if (IsIntensity)
                return (byte)(255 * intensity);

            double r = color.Red / (double)mask.Red;
            double g = color.Green / (double)mask.Green;
            double b = color.Blue / (double)mask.Blue;
            double amount = Math.Min(Math.Min(r, g), b);
            color = new ColorRGB((byte)(r - amount), (byte)(g - amount), (byte)(b - amount));
            return (byte)(amount * 255 * intensity);

        }

        public override string ToString()
        {
            return stringValue;
        }
    }

    /*public enum DmxChannel : byte
    {
        // In order of imporance
        Unknown = 0,
        Intensity = 1,
        Amber = 2,  // #ffbf00
        Indigo = 4, // #6F00FF
        Lime = 8,   // #92ff00
        Red = 16,
        Green = 32,
        Blue = 64,
        White = 128,
    }*/
}
