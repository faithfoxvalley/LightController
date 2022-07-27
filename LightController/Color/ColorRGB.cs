using System;

namespace LightController.Color
{
    public class ColorRGB
    {
        public ColorRGB() { }

        public ColorRGB(byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        /// <summary>
        /// Create color from byte array in BGR format
        /// </summary>
        public ColorRGB(byte[] data, int start)
        {
            Red = data[start + 2];
            Green = data[start + 1];
            Blue = data[start];
        }

        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }


        public static explicit operator ColorHSV(ColorRGB x)
        {
            double _red = x.Red / 255f;
            double _green = x.Green / 255f;
            double _blue = x.Blue / 255f;

            // Value
            double value = Math.Max(_red, Math.Max(_green, _blue));

            double cMin = Math.Min(_red, Math.Min(_green, _blue));
            double chroma = value - cMin;

            // Hue
            double hue;
            if (chroma == 0)
                hue = 0;
            else if (value == _red)
                hue = 60 * ((_green - _blue) / chroma);
            else if (value == _green)
                hue = 60 * (2 + (_blue - _red) / chroma);
            else if (value == _blue)
                hue = 60 * (4 + (_red - _green) / chroma);
            else
                throw new Exception("Impossible result.");

            // Saturation
            double saturation;
            if (value == 0)
                saturation = 0;
            else
                saturation = chroma / value;

            return new ColorHSV(hue, saturation, value);
        }

        public override string ToString()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}";
        }
    }
}
