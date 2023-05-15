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
        public string Hue
        {
            get => hue.ToString();
            set => hue = Percent.Parse(value, 0);
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

        public ColorHSV Color => new ColorHSV(hue.Value, sat.Value, val.Value);

        private Percent hue = new Percent(0);
        private Percent sat = new Percent(1);
        private Percent val = new Percent(1);
    }
}
