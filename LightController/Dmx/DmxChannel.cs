using Colourful;
using LightController.Color;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Dmx
{
    public class DmxChannel
    {
        private static IColorConverter<xyChromaticity, RGBColor> colourfulConverter; // Used for color temperature
        private ColorRGB mask;
        private byte? constantValue;
        private string stringValue;
        private double lumens;
        private double intensity = 1;

        public double Lumens => mask == null ? double.NaN : lumens;
        public int Index { get; private set; }
        public byte? Constant => constantValue;
        public bool IsIntensity { get; private set; } = false;
        public bool IsColor => mask != null;
        public double MaskSize => mask == null ? double.PositiveInfinity : mask.Red + mask.Green + mask.Blue;

        public DmxChannel(ColorRGB mask, string stringValue, int index, double lumens)
        {
            this.mask = mask;
            this.stringValue = stringValue;
            Index = index;
            this.lumens = lumens;
        }

        public static DmxChannel Parse(string value, int index)
        {
            if (value == null)
                return null;

            value = value.Trim().ToLowerInvariant();
            if(string.IsNullOrWhiteSpace(value))
                return null;

            string originalString = value;

            double lumens = double.NaN;
            int intensityIndex = value.IndexOf('@');
            if(intensityIndex > 0 && intensityIndex < value.Length - 1)
            {
                string intensity = value.Substring(intensityIndex + 1);
                value = value.Substring(0, intensityIndex);
                if (!double.TryParse(intensity, out lumens))
                    lumens = double.NaN;
            }

            if (value[value.Length - 1] == 'k')
            {
                if(double.TryParse(value.Substring(0, value.Length - 1), out double temperature))
                {
                    xyChromaticity chromacity = CCTConverter.GetChromaticityOfCCT(temperature);
                    if(colourfulConverter == null)
                    {
                        colourfulConverter = new ConverterBuilder()
                        .Fromxy(Illuminants.D65).ToRGB().Build();
                    }
                    RGBColor colourfulColor = colourfulConverter.Convert(chromacity).NormalizeIntensity();
                    return new DmxChannel(ColorRGB.FromColor(colourfulColor), originalString, index, lumens);
                }
                return null;
            }

            if (value[0] == '#' && value.Length == 7)
            {
                try
                {
                    byte r = (byte)Convert.ToInt32(value.Substring(1, 2), 16);
                    byte g = (byte)Convert.ToInt32(value.Substring(3, 2), 16);
                    byte b = (byte)Convert.ToInt32(value.Substring(5, 2), 16);
                    return new DmxChannel(new ColorRGB(r, g, b), originalString, index, lumens);
                }
                catch
                {
                    return null;
                }
            }

            ColorRGB mask;
            switch (value)
            {
                case "red":
                    mask = new ColorRGB(255, 0, 0);
                    break;
                case "green":
                    mask = new ColorRGB(0, 255, 0);
                    break;
                case "blue":
                    mask = new ColorRGB(0, 0, 255);
                    break;
                case "white":
                    mask = new ColorRGB(255, 255, 255);
                    break;
                case "amber":
                    mask = new ColorRGB(255, 191, 0);
                    break;
                case "intensity":
                    return new DmxChannel(null, originalString, index, lumens)
                    {
                        IsIntensity = true
                    };
                default:
                    if(byte.TryParse(value, out byte b))
                    {
                        return new DmxChannel(null, originalString, index, lumens)
                        {
                            constantValue = b
                        };
                    }
                    return null;
            }
            return new DmxChannel(mask, originalString, index, lumens);
        }

        public void ApplyIntensityMultiplier(double intensity)
        {
            this.intensity = intensity;
        }

        public double GetColorValue(ref ColorRGB color)
        {
            double r = 0;
            if (mask.Red > 0)
                r = color.Red / (double)mask.Red;
            else
                r = double.PositiveInfinity;
            double g = 0;
            if (mask.Green > 0)
                g = color.Green / (double)mask.Green;
            else
                g = double.PositiveInfinity;
            double b = 0;
            if (mask.Blue > 0)
                b = color.Blue / (double)mask.Blue;
            else
                b = double.PositiveInfinity;

            double amount = Math.Min(Math.Min(r, g), b);
            color = new ColorRGB(
                (byte)(color.Red - (amount * mask.Red)), 
                (byte)(color.Green - (amount * mask.Green)), 
                (byte)(color.Blue - (amount * mask.Blue)));
            return amount * intensity;

        }

        public override string ToString()
        {
            return stringValue;
        }
    }

    /*public enum DmxChannel : byte
    {
        // In order of imporance
        Unknown = 0,
        Intensity = 1,
        Amber = 2,  // #ffbf00
        Indigo = 4, // #6F00FF
        Lime = 8,   // #92ff00
        Red = 16,
        Green = 32,
        Blue = 64,
        White = 128,
    }*/
}
