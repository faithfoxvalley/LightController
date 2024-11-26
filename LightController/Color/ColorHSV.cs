using ColorPicker.Models;
using System;
using System.Collections.Generic;

namespace LightController.Color
{
    public class ColorHSV
    {
        public ColorHSV(double hue, double saturation, double value)
        {
			if (hue < 0)
				hue += 360;
			if (hue >= 360)
				hue -= 360;
            Hue = hue;
            Saturation = saturation;
            Value = value;
        }

        public ColorHSV(ColorHSV other) : this(other.Hue, other.Saturation, other.Value)
        {
        }

        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Value { get; set; }
		public static ColorHSV Black { get; } = new ColorHSV(0, 0, 0);

        public static explicit operator ColorRGB(ColorHSV x)
        {
			if (x == null)
				return null;

			double r, g, b;
			double hue = x.Hue;
			double saturation = x.Saturation;
			double value = x.Value;

			if (saturation == 0)
			{
				r = value;
				g = value;
				b = value;
			}
			else
			{
				int i;
				double f, p, q, t;

				if (hue == 360)
					hue = 0;
				else
					hue = hue / 60;

				i = (int)Math.Truncate(hue);
				f = hue - i;

				p = value * (1.0 - saturation);
				q = value * (1.0 - (saturation * f));
				t = value * (1.0 - (saturation * (1.0 - f)));

				switch (i)
				{
					case 0:
						r = value;
						g = t;
						b = p;
						break;

					case 1:
						r = q;
						g = value;
						b = p;
						break;

					case 2:
						r = p;
						g = value;
						b = t;
						break;

					case 3:
						r = p;
						g = q;
						b = value;
						break;

					case 4:
						r = t;
						g = p;
						b = value;
						break;

					default:
						r = value;
						g = p;
						b = q;
						break;
				}

			}

			return new ColorRGB((byte)Math.Round(r * 255), (byte)Math.Round(g * 255), (byte)Math.Round(b * 255));
		}

        public static bool operator ==(ColorHSV left, ColorHSV right)
        {
            return EqualityComparer<ColorHSV>.Default.Equals(left, right);
        }

        public static bool operator !=(ColorHSV left, ColorHSV right)
        {
            return !(left == right);
        }

        public override string ToString()
		{
			return $"{Hue:0.#}, {Saturation:P}, {Value:P}";
		}

		public static ColorHSV Lerp(ColorHSV previousColor, ColorHSV newColor, double percent)
        {
			double hue = LerpAngle(previousColor.Hue, newColor.Hue, percent);
			double sat = Lerp(previousColor.Saturation, newColor.Saturation, percent);
			double val = Lerp(previousColor.Value, newColor.Value, percent);
			return new ColorHSV(hue, sat, val);
		}

		// Interpolates between a and b by percent.
		static double Lerp(double a, double b, double percent)
		{
			return a + (b - a) * percent;
		}

		// Loops the value, so that it is never larger than length and never smaller than 0.
		static double Repeat(double value, double length)
		{
			return Math.Clamp(value - Math.Floor(value / length) * length, 0, length);
		}

		// Same as Lerp but makes sure the values interpolate correctly when they wrap around 360 degrees.
		static double LerpAngle(double a, double b, double percent)
		{
			double delta = Repeat((b - a), 360);
			if (delta > 180)
				delta -= 360;
			return a + delta * percent;
		}

        public override bool Equals(object obj)
        {
            return obj is ColorHSV hSV &&
                   Hue == hSV.Hue &&
                   Saturation == hSV.Saturation &&
                   Value == hSV.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hue, Saturation, Value);
        }

        internal static ColorHSV FromColor(ColorState colorState)
        {
			return new ColorHSV(colorState.HSV_H, colorState.HSV_S, colorState.HSV_V);
        }
    }
}
