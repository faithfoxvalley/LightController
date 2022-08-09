using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config
{
    public class Percent
    {
        public double Value { get; private set; }
        private string stringValue;

        private Percent(string valueString, double value)
        {
            Value = Math.Clamp(value, 0, 1);
            stringValue = valueString;
        }

        public static Percent Parse(string value, double defaultValue)
        {

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (value[value.Length - 1] == '%' && value.Length > 1)
                {
                    if(double.TryParse(value.Substring(0, value.Length - 1), out double percent))
                        return new Percent(value, percent / 100);
                }
                else
                {
                    if (double.TryParse(value, out double rawValue))
                        return new Percent(value, rawValue);
                }
            }

            return new Percent($"{defaultValue * 100:0.#}%", defaultValue);
        }

        public override string ToString()
        {
            return stringValue;
        }
    }
}
