using ProtoBuf;
using System;
using System.Collections.Generic;

namespace LightController.Color
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class ColorRGB : IEquatable<ColorRGB>
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

        public ColorRGB(ColorRGB other) : this(other.Red, other.Green, other.Blue)
        {
        }

        [ProtoMember(1)]
        public byte Red { get; set; }

        [ProtoMember(2)]
        public byte Green { get; set; }

        [ProtoMember(3)]
        public byte Blue { get; set; }


        public static explicit operator ColorHSV(ColorRGB x)
        {
            if (x == null)
                return null;

            double _red = x.Red / 255.0;
            double _green = x.Green / 255.0;
            double _blue = x.Blue / 255.0;

            // Value
            double value = Math.Max(_red, Math.Max(_green, _blue));

            double cMin = Math.Min(_red, Math.Min(_green, _blue));
            double chroma = value - cMin;

            // Hue
            double hue;
            if (chroma == 0)
                hue = 0;
            else if (value == _red)
                hue = ((_green - _blue) / chroma) % 6;
            else if (value == _green)
                hue = 2 + (_blue - _red) / chroma;
            else if (value == _blue)
                hue = 4 + (_red - _green) / chroma;
            else
                throw new Exception("Impossible result.");
            hue *= 60;

            // Saturation
            double saturation;
            if (value == 0)
                saturation = 0;
            else
                saturation = chroma / value;

            return new ColorHSV(hue, saturation, value);
        }

        public static bool operator ==(ColorRGB left, ColorRGB right)
        {
            return EqualityComparer<ColorRGB>.Default.Equals(left, right);
        }

        public static bool operator !=(ColorRGB left, ColorRGB right)
        {
            return !(left == right);
        }

        public static ColorRGB FromColor(System.Drawing.Color color)
        {
            return new ColorRGB(color.R, color.G, color.B);
        }
        public static ColorRGB FromColor(System.Windows.Media.Color color)
        {
            return new ColorRGB(color.R, color.G, color.B);
        }
        public static ColorRGB FromColor(Colourful.RGBColor color)
        {
            return new ColorRGB((byte)(color.R * 255), (byte)(color.G * 255), (byte)(color.B * 255));
        }

        public byte Max()
        {
            return Math.Max(Math.Max(Red, Green), Blue);
        }

        public byte Min()
        {
            return Math.Min(Math.Min(Red, Green), Blue);
        }

        /*public static explicit operator ColorHSI(ColorRGB x)
        {
            if (x == null)
                return null;

            double _red = x.Red / 255.0;
            double _green = x.Green / 255.0;
            double _blue = x.Blue / 255.0;

            double cMax = Math.Max(_red, Math.Max(_green, _blue));
            double cMin = Math.Min(_red, Math.Min(_green, _blue));
            double chroma = cMax - cMin;


            // Hue
            double hue;
            if (chroma == 0)
                hue = 0;
            else if (cMax == _red)
                hue = (_green - _blue) / chroma;
            else if (cMax == _green)
                hue = 2 + (_blue - _red) / chroma;
            else if (cMax == _blue)
                hue = 4 + (_red - _green) / chroma;
            else
                throw new Exception("Impossible result.");
            hue *= 60;
            while (hue < 0)
                hue += 360;

            // Intensity
            double intensity = (_red + _green + _blue) / 3;

            // Saturation
            double saturation = 0;
            if (intensity == 0)
                saturation = 0;
            else
                saturation = 1 - (cMin / intensity);

            return new ColorHSI(hue, saturation, intensity);
        }*/

        public override string ToString()
        {
            return $"#{Red:X2}{Green:X2}{Blue:X2}";
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColorRGB);
        }

        public bool Equals(ColorRGB other)
        {
            return other is not null &&
                   Red == other.Red &&
                   Green == other.Green &&
                   Blue == other.Blue;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Red, Green, Blue);
        }
    }
}
