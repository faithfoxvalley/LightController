using LightController.Color;
using System;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    public class AnimatedInputFrame
    {
        [YamlMember(Alias = "Length")]
        public double Length
        {
            get
            {
                return length;
            }
            set
            {
                length = value;
                LengthTime = TimeSpan.FromSeconds(value);
            }
        }
        private double length;
        [YamlIgnore]
        public TimeSpan LengthTime { get; private set; } = TimeSpan.Zero;

        [YamlMember(Alias = "Hue")]
        public double Hue
        {
            get => Color.Hue;
            set => Color.Hue = value;
        }

        [YamlMember(Alias = "Saturation")]
        public string Saturation
        {
            get => DoubleToPercent(Color.Saturation);
            set => Color.Saturation = ParsePercent(value, 1);
        }

        [YamlIgnore]
        public ColorHSV Color { get; private set; } = new ColorHSV(0, 1, 1);

        [YamlMember(Alias = "Intensity")]
        public string IntensityMode
        {

            get => DoubleToPercent(Color.Value);
            set => Color.Value = ParsePercent(value, 1);
        }

        [YamlMember(Alias = "Mix")]
        public bool Mix { get; set; } = true;

        public AnimatedInputFrame() { }


        static double ParsePercent(string value, double defaultValue)
        {
            if(value.EndsWith('%'))
            {
                if (value.Length > 1 && double.TryParse(value.Substring(0, value.Length - 1), out double percent))
                {
                    if (percent < 0)
                        return 0;
                    if (percent > 100)
                        return 1;
                    return percent / 100;
                }
                return defaultValue;
            }

            if(double.TryParse(value, out double rawValue))
            {
                if (rawValue < 0)
                    return 0;
                if (rawValue > 1)
                    return 1;
                return rawValue;
            }
            return defaultValue;
        }

        static string DoubleToPercent(double value)
        {
            return (value * 100).ToString() + "%";
        }
    }
}
