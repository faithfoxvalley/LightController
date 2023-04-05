using LightController.Color;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    [YamlTag("!animated_input")]
    public class AnimatedInput : InputBase
    {
        [YamlMember(Alias = "Colors")]
        public List<AnimatedInputFrame> Colors { get; set; }

        [YamlMember(Alias = "Loop")]
        public bool Loop { get; set; }

        private DateTime currentFrameEnds = DateTime.MaxValue;
        private int currentFrameIndex;
        private AnimatedInputFrame currentFrame;
        private object currentFrameLock = new object();

        public AnimatedInput() { }

        public override void Init()
        {
            if (Colors == null)
                Colors = new List<AnimatedInputFrame>();
        }

        public override Task StartAsync(MidiNote note)
        {
            lock(currentFrameLock)
            {
                currentFrameIndex = 0;
                if (Colors.Count == 0)
                {
                    currentFrame = null;
                    currentFrameEnds = DateTime.MaxValue;
                }
                else
                {
                    currentFrame = Colors.First();
                    currentFrameEnds = DateTime.UtcNow + currentFrame.LengthTime;
                }
            }
            return Task.CompletedTask;
        }

        public override Task UpdateAsync()
        {
            DateTime now = DateTime.UtcNow;
            lock (currentFrameLock)
            {
                if (currentFrame != null && currentFrameIndex >= 0 && currentFrameIndex < Colors.Count)
                {
                    if (now > currentFrameEnds)
                    {
                        currentFrameIndex++;
                        if (currentFrameIndex < Colors.Count)
                        {
                            currentFrame = Colors[currentFrameIndex];
                            currentFrameEnds += currentFrame.LengthTime;
                        }
                        else if(Loop)
                        {
                            currentFrameIndex = 0;
                            currentFrame = Colors.First();
                            currentFrameEnds += currentFrame.LengthTime;
                        }
                        else
                        {
                            currentFrameEnds = DateTime.MaxValue;
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }

        public override ColorHSV GetColor(int fixtureId)
        {
            ColorHSV currentColor;
            lock (currentFrameLock)
            {
                if (currentFrame == null)
                    currentColor = ColorHSV.Black;
                else
                    currentColor = (ColorHSV)currentFrame.RGB;
            }
            return currentColor;
        }

        public override double GetIntensity(int fixtureid, ColorHSV target)
        {
            double? currentIntensity = null;
            lock (currentFrameLock)
            {
                if (currentFrame == null)
                    currentIntensity = 0;
                else if (currentFrame.Intensity.HasValue)
                    currentIntensity = currentFrame.Intensity.Value;
            }
            if (currentIntensity.HasValue)
                return currentIntensity.Value;
            return intensity.GetIntensity(target);
        }
    }
}
