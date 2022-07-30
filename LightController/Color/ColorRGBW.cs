using System;

namespace LightController.Color
{
    public class ColorRGBW
    {
        public ColorRGBW() { }

        public ColorRGBW(byte red, byte green, byte blue, byte white)
        {
            Red = red;
            Green = green;
            Blue = blue;
            White = white;
        }

        /// <summary>
        /// Create color from byte array in BGR format
        /// </summary>
        public ColorRGBW(byte[] data, int start)
        {
            Red = data[start + 2];
            Green = data[start + 1];
            Blue = data[start];
        }

        public byte Red { get; set; }
        public byte Green { get; set; }
        public byte Blue { get; set; }
        public byte White { get; set; }


        public override string ToString()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}";
        }
    }
}
