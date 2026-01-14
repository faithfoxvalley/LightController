using LightController.Config;
using System;
using YamlDotNet.Serialization;

namespace LightController.Color
{
    public class SerializableColorHSV
    {
        public double Hue
        {
            get => color.Hue;
            set => color.Hue = Math.Clamp(value, 0, 360);
        }

        public string Saturation
        {
            get
            {
                return sat.ToString();
            }

            set
            {
                sat = Percent.Parse(value, 1);
                color.Saturation = sat.Value;
            }
        }

        public string Value
        {
            get
            {
                return val.ToString();
            }

            set
            {
                val = Percent.Parse(value, 1);
                color.Value = val.Value;
            }
        }

        [YamlIgnore]
        public ColorHSV Color => color;

        private Percent sat = new Percent(1);
        private Percent val = new Percent(1);
        private readonly ColorHSV color = new ColorHSV(0, 1, 1);

        public SerializableColorHSV() { }

        public SerializableColorHSV(ColorHSV color)
        {
            this.color = new ColorHSV(color);
            sat = new Percent(color.Saturation);
            val = new Percent(color.Value);
        }
    }
}
