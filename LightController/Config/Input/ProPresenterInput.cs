using LightController.Color;
using LightController.Pro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LightController.Config.Input
{
    [YamlTag("!propresenter_input")]
    public class ProPresenterInput : InputBase
    {
        private ProPresenter pro = null;
        private ProMediaItem media;
        private ColorRGB[] colors;
        private byte maxColorValue;
        private byte minColorValue;
        private int pixelWidth;
        private object colorLock = new object();
        private InputIntensity minIntensity = new InputIntensity();
        private static CancellationTokenSource cts = new CancellationTokenSource();
        private Dictionary<int, int> idToIndex = new Dictionary<int, int>();

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

            int count = 0;
            foreach(int id in FixtureIds.EnumerateValues())
            {
                idToIndex[id] = count;
                count++;
            }
            pixelWidth = count;
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
                            colors = media.GetData(pixelWidth, 0);
                            maxColorValue = colors.Select(x => x.Max()).Max();
                            minColorValue = colors.Select(x => x.Max()).Min();
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    LogFile.Error("Unable to communicate with ProPresenter");
                }
                catch (OperationCanceledException)
                {
                    LogFile.Info("Canceled media/thumbnail generation.");
                }

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
                colors = media.GetData(pixelWidth, time);
                maxColorValue = colors.Select(x => x.Max()).Max();
                minColorValue = colors.Select(x => x.Max()).Min();
            }
        }

        public override ColorRGB GetColor(int fixtureId)
        {
            int index = idToIndex[fixtureId];
            ColorRGB result;
            lock(colorLock)
            {
                if (colors == null)
                {
                    result = new ColorRGB();
                }
                else 
                {
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
