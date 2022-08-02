using LightController.Color;
using System;

namespace LightController.Config.Input
{
    public class InputIntensity
    {
        private double? intensity;

        public static InputIntensity Parse(string value)
        {
            if (value == null)
                return new InputIntensity();

            value = value.Trim().ToLowerInvariant();
            if (value == "auto")
                return new InputIntensity();

            InputIntensity result = new InputIntensity();

            if (byte.TryParse(value, out byte intensity))
            {
                result.intensity = intensity / 255d;
                return result;
            }

            // TODO: Warn
            return result;
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
            return intensity.HasValue ? intensity.Value.ToString() : "auto";
        }
    }
}