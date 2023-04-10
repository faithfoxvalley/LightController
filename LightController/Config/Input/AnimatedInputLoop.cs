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
        private DateTime frameStart;
        private DateTime mixStart;
        private DateTime frameEnd;
        private TimeSpan startDelay;
        private int frameIndex;
        private AnimatedInputFrame frame;
        private ColorHSV previousColor;
        private ColorHSV color;
        private object colorLock = new object();

        public AnimatedInputLoop(bool loop, List<AnimatedInputFrame> frames, double delay)
        {
            this.loop = loop;
            this.frames = frames;
            startDelay = TimeSpan.FromSeconds(delay);
            Reset();
        }

        public void Reset()
        {
            frameIndex = 0;
            previousColor = null;
            mixStart = DateTime.UtcNow;
            if (frames.Count == 0)
            {
                frame = null;
                frameStart = DateTime.MaxValue;
                frameEnd = DateTime.MaxValue;
            }
            else
            {
                frame = frames.First();
                frameStart = mixStart + startDelay;
                frameEnd = frameStart + frame.LengthTime;
            }

            lock(colorLock)
            {
                color = null;
            }
        }

        public void Update()
        {
            if (frame == null)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < frameStart)
            {
                TimeSpan mixLength = frameStart - mixStart;
                TimeSpan mixTime = now - mixStart;
                double percent = mixTime / mixLength;
                MixColors(percent);
            }
            else if (now > frameEnd)
            {
                AdvanceFrame();
            }
        }

        private void MixColors(double percent)
        {
            if (previousColor == null)
            {
                lock (colorLock)
                {
                    color = null;
                }
            }
            else
            {
                ColorHSV newColor = frame.Color;
                // TODO: Color interpolation
                // Hint: Unity Mathf.LerpAngle
            }

        }

        private void AdvanceFrame()
        {
            mixStart = frameEnd;
            frameStart = frameEnd + frame.MixLengthTime;
            previousColor = frame.Color;
            frameIndex++;
            if (frameIndex < frames.Count)
            {
                frame = frames[frameIndex];
                frameEnd = frameStart + frame.LengthTime;
            }
            else if (loop)
            {
                frameIndex = 0;
                frame = frames.First();
                frameEnd = frameStart + frame.LengthTime;
            }
            else
            {
                frame = null;
            }
        }

        public ColorHSV GetColor()
        {
            ColorHSV currentColor;
            lock (colorLock)
            {
                currentColor = color ?? ColorHSV.Black;
            }
            return currentColor;
        }

        public double GetIntensity()
        {
            double currentIntensity;
            lock (colorLock)
            {
                currentIntensity = color?.Value ?? 0;
            }
            return currentIntensity;
        }
    }
}
