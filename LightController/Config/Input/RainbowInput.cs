using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config.Input
{
    [YamlTag("!rainbow_input")]
    public class RainbowInput : InputBase
    {
        /// <summary>
        /// From 0 to 100
        /// </summary>
        public string Saturation
        {
            get => saturation.ToString();
            set => saturation = Percent.Parse(value, 1);
        }

        /// <summary>
        /// Cycle length in seconds
        /// </summary>
        public double CycleLength { get; set; }

        private Percent saturation = new Percent(1);
        private DateTime startTime;

        public override void Init()
        {
            startTime = DateTime.Now;
        }

        public override ColorHSV GetColor(int fixtureId)
        {
            double currentPosition = ((DateTime.Now - startTime).TotalSeconds) % CycleLength;
            double hue = (currentPosition / CycleLength) * 360;
            return new ColorHSV(hue, saturation.Value, 1);
            
        }
    }
}
