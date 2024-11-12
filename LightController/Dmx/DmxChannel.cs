﻿using Colourful;
using LightController.Color;
using System;

namespace LightController.Dmx
{
    public class DmxChannel
    {
        private static IColorConverter<xyChromaticity, RGBColor> colourfulConverter; // Used for color temperature
        private ColorRGB mask;
        private byte? constantValue;
        private string stringValue;
        private double intensity = 1;
        private double intensityMin = 0;
        private double intensityRange = 1;

        public int Index { get; private set; }
        public byte? Constant => constantValue;
        public bool IsIntensity { get; private set; } = false;
        public bool IsColor => mask != null;
        public double MaskSize => mask == null ? double.PositiveInfinity : mask.Red + mask.Green + mask.Blue;

        public DmxChannel(ColorRGB mask, string stringValue, int index)
        {
            this.mask = mask;
            this.stringValue = stringValue;
            Index = index;
        }

        public void ApplyIntensityModifier(double minIntensity)
        {
            intensityMin = minIntensity;
            intensityRange = 1 - intensityMin;
        }

        public static DmxChannel Parse(string value, int index)
        {
            if (value == null)
                return null;

            value = value.Trim().ToLowerInvariant();
            if(string.IsNullOrWhiteSpace(value))
                return null;

            string originalString = value;
            if(value.StartsWith("intensity"))
            {
                DmxChannel intensityResult = new DmxChannel(null, originalString, index)
                {
                    IsIntensity = true
                };

                int minIntensityIndex = value.IndexOf('>');
                if (minIntensityIndex > 0 && minIntensityIndex < value.Length - 1 
                    && double.TryParse(value.Substring(minIntensityIndex + 1), out double minIntensity)
                    && minIntensity > 0)
                {
                    if (minIntensity > 255)
                        minIntensity = 255;
                    intensityResult.ApplyIntensityModifier(minIntensity / 255);
                }
                return intensityResult;
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
                    return new DmxChannel(ColorRGB.FromColor(colourfulColor), originalString, index);
                }
                LogFile.Warn("'" + originalString + "' is not a supported color temperature.");
                return null;
            }

            if (value[0] == '#' && value.Length == 7)
            {
                try
                {
                    byte r = (byte)Convert.ToInt32(value.Substring(1, 2), 16);
                    byte g = (byte)Convert.ToInt32(value.Substring(3, 2), 16);
                    byte b = (byte)Convert.ToInt32(value.Substring(5, 2), 16);
                    return new DmxChannel(new ColorRGB(r, g, b), originalString, index);
                }
                catch
                {
                    LogFile.Warn("'" + originalString + "' is not a supported hex color value.");
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
                    mask = new ColorRGB(255, 126, 0);
                    break;
                case "intensity": // This probably wont ever be triggered
                    return new DmxChannel(null, originalString, index)
                    {
                        IsIntensity = true
                    };
                default:
                    if(byte.TryParse(value, out byte b))
                    {
                        return new DmxChannel(null, originalString, index)
                        {
                            constantValue = b
                        };
                    }
                    LogFile.Warn("'" + originalString + "' is not a supported color value.");
                    return null;
            }
            return new DmxChannel(mask, originalString, index);
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

        internal double GetIntensityByte(double intensity)
        {
            if (intensity <= 0)
                return 0;
            if (intensityMin > 0)
                intensity = (intensity * intensityRange) + intensityMin;
            return intensity * 255;
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
