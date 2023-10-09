using LightController.Color;
using static LightController.Pro.Packet.TransportLayerStatus;
using YamlDotNet.Core.Tokens;
using System.Collections.Generic;
using System;
using System.Linq;

namespace LightController.Config.Input
{
    public class InputIntensity
    {
        private readonly Dictionary<int, double> intensityMap = new Dictionary<int, double>();
        private string intensityString;
        private double intensity;

        public InputIntensity(double intensity)
        {
            this.intensity = intensity;
            intensityString = $"{intensity * 100:0.#}%";
        }

        public static InputIntensity Parse(string value, double defaultValue)
        {
            InputIntensity result = new InputIntensity(defaultValue);

            if (string.IsNullOrWhiteSpace(value))
                return result;

            string[] args = value.Split(';');
            foreach(string arg in args)
            {
                int colon = arg.IndexOf(':');
                if(colon >= 0 && colon < arg.Length - 1)
                {
                    string percent = arg.Substring(colon + 1);
                    if(TryParseIntensityRange(percent, out double minIntensity, out double maxIntensity))
                    {
                        string fixtureIds = arg.Substring(0, colon);
                        ValueSet fixtureIdSet;
                        try
                        {
                            fixtureIdSet = new ValueSet(fixtureIds);
                            if(minIntensity == maxIntensity)
                            {
                                foreach(int id in fixtureIdSet.EnumerateValues())
                                    result.intensityMap[id] = minIntensity;
                            }
                            else
                            {
                                int[] fixtures = fixtureIdSet.EnumerateValues().ToArray();
                                for (int i = 0; i < fixtures.Length; i++)
                                {
                                    int id = fixtures[i];
                                    if (fixtures.Length > 1)
                                    {
                                        double lerpPercent = (double)i / (fixtureIds.Length - 1);
                                        result.intensityMap[id] = Lerp(minIntensity, maxIntensity, lerpPercent);
                                    }
                                    else
                                    {
                                        result.intensityMap[id] = maxIntensity;
                                    }
                                }
                            }
                        }
                        catch (Exception e) 
                        {
                            
                        }
                        continue;
                    }
                }

                if(TryParseIntensity(arg, out double defaultIntensity))
                    result.intensity = defaultIntensity;
                else
                    LogFile.Warn("'" + arg + "' is not a valid intensity.");
            }

            result.intensityString = value;
            return result;
        }

        private static double Lerp(double a, double b, double percent)
        {
            // a = 5, b = 27, per = 0.14
            return a + (b - a) * percent;
        }

        private static bool TryParseIntensityRange(string value, out double minIntensity, out double maxIntensity)
        {
            int dashIndex = value.IndexOf('-');
            minIntensity = 0;
            maxIntensity = 0;

            if (dashIndex >= value.Length - 1)
                return false;

            if(dashIndex < 0)
            {
                if(TryParseIntensity(value, out double intensity))
                {
                    minIntensity = intensity;
                    maxIntensity = intensity;
                    return true;
                }
                return false;
            }

            string minString = value.Substring(0, dashIndex);
            if (!TryParseIntensity(minString, out minIntensity))
                return false;

            string maxString = value.Substring(dashIndex + 1);
            if (!TryParseIntensity(maxString, out maxIntensity))
                return false;

            return true;
        }

        private static bool TryParseIntensity(string value, out double intensity)
        {
            if (value.EndsWith('%'))
            {
                if (double.TryParse(value.Substring(0, value.Length - 1), out double percent)
                    && !double.IsNaN(percent) && !double.IsInfinity(percent) && percent >= 0 && percent <= 100)
                {
                    intensity = percent / 100d;
                    return true;
                }
            }
            else if (byte.TryParse(value, out byte intensityByte))
            {
                intensity = intensityByte / 255d;
                return true;
            }

            intensity = 0;
            return false;
        }

        public double GetIntensity(int fixtureId)
        {
            if (intensityMap.TryGetValue(fixtureId, out double fixtureIntensity))
                return fixtureIntensity;
            return intensity;
        }

        public override string ToString()
        {
            return intensityString;
        }
    }
}