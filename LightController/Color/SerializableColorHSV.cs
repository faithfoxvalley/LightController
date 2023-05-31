using LightController.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Color
{
    public class SerializableColorHSV
    {
        public double Hue
        {
            get => hue;
            set => hue = Math.Clamp(value, 0, 360);
        }

        public string Saturation
        {
            get => sat.ToString();
            set => sat = Percent.Parse(value, 1);
        }

        public string Value
        {
            get => val.ToString();
            set => val = Percent.Parse(value, 1);
        }

        public ColorHSV Color => new ColorHSV(hue, sat.Value, val.Value);

        private double hue;
        private Percent sat = new Percent(1);
        private Percent val = new Percent(1);
    }
}
