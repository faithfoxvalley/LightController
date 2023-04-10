using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightController.Config.Input
{
    public class AnimatedInputLoop
    {
        private bool loop;
        private readonly List<AnimatedInputFrame> frames;
        private DateTime frameEnds = DateTime.MaxValue;
        private int frameIndex;
        private AnimatedInputFrame frame;
        private object frameLock = new object();

        public AnimatedInputLoop(bool loop, List<AnimatedInputFrame> frames)
        {
            this.loop = loop;
            this.frames = frames;
        }

        public void Reset()
        {
            lock (frameLock)
            {
                frameIndex = 0;
                if (frames.Count == 0)
                {
                    frame = null;
                    frameEnds = DateTime.MaxValue;
                }
                else
                {
                    frame = frames.First();
                    frameEnds = DateTime.UtcNow + frame.LengthTime;
                }
            }
        }

        public void Update()
        {
            DateTime now = DateTime.UtcNow;
            lock (frameLock)
            {
                if (frame != null && frameIndex >= 0 && frameIndex < frames.Count)
                {
                    if (now > frameEnds)
                    {
                        frameIndex++;
                        if (frameIndex < frames.Count)
                        {
                            frame = frames[frameIndex];
                            frameEnds += frame.LengthTime;
                        }
                        else if (loop)
                        {
                            frameIndex = 0;
                            frame = frames.First();
                            frameEnds += frame.LengthTime;
                        }
                        else
                        {
                            frameEnds = DateTime.MaxValue;
                        }
                    }
                }
            }
        }

        public ColorHSV GetColor()
        {
            ColorHSV currentColor;
            lock (frameLock)
            {
                if (frame == null)
                    currentColor = ColorHSV.Black;
                else
                    currentColor = frame.Color;
            }
            return currentColor;
        }

        public double GetIntensity()
        {
            double currentIntensity;
            lock (frameLock)
            {
                if (frame == null)
                    currentIntensity = 0;
                else
                    currentIntensity = frame.Color.Value;
            }
            return currentIntensity;
        }
    }
}
