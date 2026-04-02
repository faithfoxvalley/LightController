using LightController.Color;
using System.Collections.Generic;
using System.Linq;

namespace LightController.Dmx;

public class DmxFixture
{
    private DmxFrame frame;
    private DmxChannel intensityChannel;
    private List<DmxChannel> colorChannels = new List<DmxChannel>();
    private readonly List<Config.Input.InputBase> inputs = new List<Config.Input.InputBase>();
    private readonly List<ColorHSV> inputProcessingCache = new List<ColorHSV>();
    private bool disabled;
    private bool newInput;
    private object inputLock = new object();
    private int fixtureId;
    private double mixLength, mixDelay;
    private string detailString;

    public int FixtureId => fixtureId;


    public DmxFixture(Config.Dmx.DmxDeviceProfile profile, int dmxStartAddress, int fixtureId, int universe)
    {
        detailString = $"{fixtureId} - {profile.Name} - {dmxStartAddress}-{dmxStartAddress + profile.DmxLength - 1}";
        if (universe > 1)
            detailString += $"[{universe}]";

        // Construct the default frame
        byte[] data = new byte[profile.DmxLength];
        
        foreach(DmxChannel channel in profile.AddressMap)
        {
            if (channel != null)
            {
                if (channel.Constant.HasValue)
                    data[channel.Index] = channel.Constant.Value;
                else if (channel.IsIntensity)
                    intensityChannel = channel;
                else if(channel.IsColor)
                    colorChannels.Add(channel);
            }
        }

        frame = new DmxFrame(data, dmxStartAddress);
        colorChannels = colorChannels.OrderByDescending(x => x.MaskSize).ToList();

        this.fixtureId = fixtureId;
    }

    public void TurnOff()
    {
        lock(inputLock)
        {
            disabled = true;
            inputs.Clear();
            newInput = true;
        }
    }

    public void SetInput(IEnumerable<Config.Input.InputBase> inputs, double mixLength, double mixDelay)
    {
        if (double.IsNaN(mixLength) || double.IsInfinity(mixLength) || mixLength < 0)
            mixLength = 0;

        lock (inputLock)
        {
            if (disabled)
                return;

            this.inputs.Clear();

            this.mixLength = mixLength;
            this.mixDelay = mixDelay;
            newInput = true;

            foreach (var input in inputs)
            {
                if (input.FixtureIds.Contains(fixtureId))
                    this.inputs.Add(input);
            }

        }

    }

    public DmxFrame GetFrame()
    {
        ColorRGB rgb = new ColorRGB();
        double intensity = 0;

        bool hasIntensity = intensityChannel != null;

        lock (inputLock)
        {
            if (disabled)
            {
                frame.Reset();
                return frame;
            }

            if (newInput)
            {
                frame.StartMix(mixLength, mixDelay);
                newInput = false;
            }

            frame.Reset();

            if (inputs.Count == 0)
            {
                frame.Mix();
                return frame;
            }

            foreach (var input in inputs)
            {
                ColorHSV inputHsv = input.GetColor(fixtureId);
                double inputIntensity = input.GetIntensity(fixtureId, inputHsv);
                if (inputIntensity <= 0)
                    continue;
                intensity += inputIntensity;
                rgb += inputHsv.ToRgbBright(inputIntensity);
            }
        }

        if (intensity > 1)
            intensity = 1;

        if (hasIntensity)
        {
            rgb.Normalize();
            frame.Set(intensityChannel.Index, intensityChannel.GetIntensityByte(intensity));
        }

        frame.SetPreviewData(rgb, intensity);

        foreach (DmxChannel channel in colorChannels)
        {
            double value = channel.GetColorValue(ref rgb) * 255;
            if (!hasIntensity)
                value *= intensity;
            frame.Set(channel.Index, value);
        }

        frame.Clamp(colorChannels.Select(x => x.Index));
        frame.Mix();

        return frame;
    }

    public override string ToString()
    {
        return detailString;
    }
}
