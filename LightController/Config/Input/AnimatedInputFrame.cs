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
        public TimeSpan LengthTime { get; private set; }


        [YamlMember(Alias = "rgb", ApplyNamingConventions = false)]
        public ColorRGB RGB { get; set; }

        [YamlMember(Alias = "Intensity")]
        public string IntensityMode
        {
            get
            {
                return intensity.ToString();
            }
            set
            {
                if (value == null)
                    intensity = new InputIntensity();
                else
                    intensity = InputIntensity.Parse(value);
            }
        }
        private InputIntensity intensity = new InputIntensity();
        public double? Intensity => intensity.Value;

        public AnimatedInputFrame() { }

    }
}
