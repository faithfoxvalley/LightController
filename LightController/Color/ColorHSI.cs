using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Color
{
    public class ColorHSI
    {
        public double Hue { get; set; }
        public double Saturation { get; set; }
        public double Intensity { get; set; }

        public ColorHSI() { }

        public ColorHSI(double hue, double saturation, double intensity)
        {
            Hue = hue;
            Saturation = saturation;
            Intensity = intensity;
        }

        // https://www.neltnerlabs.com/saikoled/how-to-convert-from-hsi-to-rgb-white
        public static explicit operator ColorRGB(ColorHSI hsi)
        {
            double r, g, b;
            double cos_h, cos_1047_h;
            double H = hsi.Hue % 360; // cycle H around to 0-360 degrees
            H *= Math.PI / 180; // Convert to radians.
            double S = Math.Clamp(hsi.Saturation, 0, 1); // clamp S and I to interval [0,1]
            double I = Math.Clamp(hsi.Intensity, 0, 1);

            double pi2_3 = 2 * Math.PI / 3.0;

            // Math! Thanks in part to Kyle Miller.
            if (H < pi2_3)
            {
                cos_h = Math.Cos(H);
                cos_1047_h = Math.Cos(1.047196667 - H);
                r = 255 * I / 3 * (1 + S * cos_h / cos_1047_h);
                g = 255 * I / 3 * (1 + S * (1 - cos_h / cos_1047_h));
                b = 255 * I / 3 * (1 - S);
            }
            else if (H < pi2_3 * 2)
            {
                H -= pi2_3;
                cos_h = Math.Cos(H);
                cos_1047_h = Math.Cos(1.047196667 - H);
                g = 255 * I / 3 * (1 + S * cos_h / cos_1047_h);
                b = 255 * I / 3 * (1 + S * (1 - cos_h / cos_1047_h));
                r = 255 * I / 3 * (1 - S);
            }
            else
            {
                H -= pi2_3 * 2;
                cos_h = Math.Cos(H);
                cos_1047_h = Math.Cos(1.047196667 - H);
                b = 255 * I / 3 * (1 + S * cos_h / cos_1047_h);
                r = 255 * I / 3 * (1 + S * (1 - cos_h / cos_1047_h));
                g = 255 * I / 3 * (1 - S);
            }

            return new ColorRGB((byte)r, (byte)g, (byte)b);
        }

        // https://www.neltnerlabs.com/saikoled/how-to-convert-from-hsi-to-rgb-white
        public static explicit operator ColorRGBW(ColorHSI hsi)
        {
            double r, g, b, w;
            double cos_h, cos_1047_h;
            double H = hsi.Hue % 360; // cycle H around to 0-360 degrees
            H *= Math.PI / 180; // Convert to radians.
            double S = Math.Clamp(hsi.Saturation, 0, 1); // clamp S and I to interval [0,1]
            double I = Math.Clamp(hsi.Intensity, 0, 1);

            double pi2_3 = 2 * Math.PI / 3.0;

            if (H < pi2_3)
            {
                cos_h = Math.Cos(H);
                cos_1047_h = Math.Cos(1.047196667 - H);
                r = S * 255 * I / 3 * (1 + cos_h / cos_1047_h);
                g = S * 255 * I / 3 * (1 + (1 - cos_h / cos_1047_h));
                b = 0;
                w = 255 * (1 - S) * I;
            }
            else if (H < pi2_3 * 2)
            {
                H -= pi2_3;
                cos_h = Math.Cos(H);
                cos_1047_h = Math.Cos(1.047196667 - H);
                g = S * 255 * I / 3 * (1 + cos_h / cos_1047_h);
                b = S * 255 * I / 3 * (1 + (1 - cos_h / cos_1047_h));
                r = 0;
                w = 255 * (1 - S) * I;
            }
            else
            {
                H -= pi2_3 * 2;
                cos_h = Math.Cos(H);
                cos_1047_h = Math.Cos(1.047196667 - H);
                b = S * 255 * I / 3 * (1 + cos_h / cos_1047_h);
                r = S * 255 * I / 3 * (1 + (1 - cos_h / cos_1047_h));
                g = 0;
                w = 255 * (1 - S) * I;
            }

            return new ColorRGBW((byte)r, (byte)g, (byte)b, (byte)w);
        }
    }
}
