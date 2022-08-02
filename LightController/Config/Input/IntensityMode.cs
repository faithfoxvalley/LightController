using LightController.Color;
using System;

namespace LightController.Config.Input
{
    public class InputIntensity
    {
        private string intensityString = "auto";
        private double? intensity;

        public InputIntensity() { }

        public InputIntensity(double intensity, string intensityString)
        {
            this.intensityString = intensityString;
            this.intensity = intensity;
        }

        public static InputIntensity Parse(string value)
        {
            if (value == null)
                return new InputIntensity();

            string stringValue = value;
            value = value.Trim().ToLowerInvariant();
            if (value == "auto")
                return new InputIntensity();

            if(value.EndsWith('%'))
            {
                if(double.TryParse(value.Substring(0, value.Length - 1), out double percent) 
                    && !double.IsNaN(percent) && !double.IsInfinity(percent) && percent > 0 && percent <= 100)
                {
                    return new InputIntensity(percent / 100d, stringValue);
                }
            }
            else if (byte.TryParse(value, out byte intensity))
            {
                return new InputIntensity(intensity / 255d, stringValue);
            }

            // TODO: Warn
            return new InputIntensity();
        }

        public double GetIntensity(ColorRGB rgb)
        {
            if (intensity.HasValue)
                return intensity.Value;

            byte value = Math.Max(Math.Max(rgb.Red, rgb.Green), rgb.Blue);
            return value / 255d;
        }

        public override string ToString()
        {
            return intensity.HasValue ? intensityString : "auto";
        }
    }
}