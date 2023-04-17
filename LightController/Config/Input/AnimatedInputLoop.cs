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
        private bool isTargetColor;
        private object colorLock = new object();

        public AnimatedInputLoop(bool loop, List<AnimatedInputFrame> frames, double delay)
        {
            this.loop = loop;
            this.frames = frames;
            startDelay = TimeSpan.FromSeconds(delay);
            TimeSpan totalLength = GetLength();
            if (startDelay > totalLength)
                startDelay = new TimeSpan(startDelay.Ticks % totalLength.Ticks);
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
            else if(!loop || startDelay.Ticks <= 0)
            {
                frame = frames.First();
                frameStart = mixStart + startDelay;
                frameEnd = frameStart + frame.LengthTime;
            }
            else
            {
                ResetFrameToTime(GetLength() - startDelay);
            }

            lock(colorLock)
            {
                color = null;
                isTargetColor = false;
            }
        }

        private void ResetFrameToTime(TimeSpan elapsedTime)
        {
            int i = 0;
            while(elapsedTime.Ticks > 0)
            {
                AnimatedInputFrame frame = frames[i];
                elapsedTime -= frame.LengthTime;
                if(elapsedTime.Ticks <= 0)
                {
                    // The current state is in the middle of a frame
                    this.frame = frame;
                    frameStart = DateTime.UtcNow + elapsedTime;
                    frameEnd = frameStart + frame.LengthTime;
                    return;
                }
                elapsedTime -= frame.MixLengthTime;
                if (elapsedTime.Ticks <= 0)
                {
                    // The current state is mixing between two frames
                    previousColor = frame.Color;
                    this.frame = frames[(i + 1) % frames.Count];
                    mixStart = DateTime.UtcNow + elapsedTime;
                    frameStart = mixStart + this.frame.MixLengthTime;
                    frameEnd = frameStart + this.frame.LengthTime;
                    return;
                }
            }
            throw new Exception("Unknown frame state");
        }

        private TimeSpan GetLength()
        {
            TimeSpan span = new TimeSpan(0);
            foreach (AnimatedInputFrame frame in frames)
                span += frame.LengthTime + frame.MixLengthTime;
            return span;
        }

        public void Update()
        {
            if (frame == null)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < frameStart)
            {
                TimeSpan mixLength = frameStart - mixStart;
                TimeSpan elapsedTime = now - mixStart;
                double percent = elapsedTime / mixLength;
                MixColors(percent);
            }
            else if (now > frameEnd)
            {
                AdvanceFrame();
            }
            else
            {
                lock(colorLock)
                {
                    if(color == null || !isTargetColor)
                    {
                        color = new ColorHSV(frame.Color);
                        isTargetColor = true;
                    }
                }
            }
        }

        private void MixColors(double percent)
        {
            if (previousColor == null)
            {
                lock (colorLock)
                {
                    color = null;
                    isTargetColor = false;
                }
            }
            else
            {
                ColorHSV lerpColor = ColorHSV.Lerp(previousColor, frame.Color, percent);
                lock (colorLock)
                {
                    color = lerpColor;
                    isTargetColor = false;
                }
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

            lock (colorLock)
            {
                isTargetColor = false;
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
