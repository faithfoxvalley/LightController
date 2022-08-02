using LightController.Color;
using System;

namespace LightController.Config.Input
{
    public class InputIntensity
    {
        private string stringValue;
        private double? intensity;

        public static InputIntensity Parse(string value)
        {
            if (value == null)
                return new InputIntensity();

            InputIntensity result = new InputIntensity();
            result.stringValue = value;

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
            return stringValue;
        }
    }
}