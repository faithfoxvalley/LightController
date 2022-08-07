using LightController.Color;
using LightController.Pro;
using MediaToolkit.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LightController.Config.Input
{
    // TODO
    [YamlTag("!propresenter_input")]
    public class ProPresenterInput : InputBase
    {
        private ProPresenter pro = null;
        private ProMediaItem media;
        private ColorRGB[] colors;
        private byte maxColorValue;
        private byte minColorValue;
        private object colorLock = new object();
        private int min;
        private int max;
        private InputIntensity minIntensity = new InputIntensity();
        private static CancellationTokenSource cts = new CancellationTokenSource();

        public bool HasMotion { get; set; } = true;

        public string MinIntensity
        {
            get
            {
                return minIntensity.ToString();
            }
            set
            {
                minIntensity = InputIntensity.Parse(value);
            }
        }

        public ProPresenterInput() { }


        public override void Init()
        {
            pro = MainWindow.Instance.Pro;
            min = FixtureIds.Min();
            max = FixtureIds.Max();
        }


        public override async Task StartAsync()
        {
            if(cts != null)
                cts.Cancel();

            // Initialize info about current background

            using (var myCts = new CancellationTokenSource())
            {
                cts = myCts;

                try
                {
                    ProMediaItem newMedia = await pro.GetCurrentMediaAsync(HasMotion, cts.Token);
                    media = newMedia;
                    if (!HasMotion)
                    {
                        lock (colorLock)
                        {
                            colors = media.GetData((max - min) + 1, 0);
                            maxColorValue = colors.Select(x => x.Max()).Max();
                            minColorValue = colors.Select(x => x.Max()).Min();
                        }
                    }
                }
                catch (OperationCanceledException)
                { }

                if (cts == myCts)
                    cts = null;

            }

        }

        public override async Task UpdateAsync()
        {
            // Update the current color based on the background frame and estimated time
            if (media == null || !HasMotion)
                return;

            double time = await pro.AsyncGetTransportLayerTime(Layer.Presentation);

            lock(colorLock)
            {
                colors = media.GetData((max - min) + 1, time);
                maxColorValue = colors.Select(x => x.Max()).Max();
                minColorValue = colors.Select(x => x.Max()).Min();
            }
        }

        public override ColorRGB GetColor(int fixtureId)
        {
            ColorRGB result;
            lock(colorLock)
            {
                if (colors == null)
                {
                    result = new ColorRGB();
                }
                else
                {
                    int index = fixtureId - min;
                    if (index >= colors.Length)
                        index = colors.Length - 1;
                    result = colors[index];
                }
            }
            return result;
        }

        public override double GetIntensity(int fixtureid, ColorRGB color)
        {
            // intensity provided by user
            double targetMaxIntensity = intensity.Value ?? 1;
            double targetMinIntensity = minIntensity.Value ?? 0;

            // intensity of the media 
            double maxChannelValue;
            double minChannelValue;
            lock (colorLock)
            {
                maxChannelValue = maxColorValue / 255d;
                minChannelValue = minColorValue / 255d;
            }

            double thisIntensity = color.Max() / 255d;

            if(minChannelValue == maxChannelValue || targetMinIntensity == targetMaxIntensity)
                return targetMaxIntensity;

            // https://math.stackexchange.com/a/914843
            // Input: [minChannelValue-maxChannelValue]
            // Output: [targetMinIntensity-targetMaxIntensity]
            double target = targetMinIntensity + (((targetMaxIntensity - targetMinIntensity) / (maxChannelValue - minChannelValue)) * (thisIntensity - minChannelValue));
            return target;
        }

    }
}
