using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightController.ColorAnimation
{
    public class AnimationLoop
    {
        private bool loop;
        private readonly List<AnimationFrame> frames = new List<AnimationFrame>();
        private DateTime startTime;
        private TimeSpan delay;
        private TimeSpan totalLength;
        private AnimationFrame dummyFrame = new AnimationFrame(ColorHSV.Black, ColorHSV.Black, TimeSpan.Zero, TimeSpan.Zero); // Used for comparisons

        private ColorHSV color;
        private object colorLock = new object();

        public AnimationLoop(bool loop, List<Config.Input.AnimatedInputFrame> frames, TimeSpan delay)
        {
            this.loop = loop;
            TimeSpan start = TimeSpan.Zero;
            for (int i = 0; i < frames.Count; i++)
            {
                Config.Input.AnimatedInputFrame frame = frames[i];
                if(frame.Length > 0)
                {
                    ColorHSV nextColor;
                    if (!frame.Mix)
                    {
                        nextColor = frame.Color;
                    }
                    else if (i == frames.Count - 1)
                    {
                        if (loop)
                            nextColor = frames[0].Color;
                        else
                            nextColor = ColorHSV.Black;
                    }
                    else
                    {
                        nextColor = frames[i + 1].Color;
                    }
                    this.frames.Add(new AnimationFrame(frame.Color, nextColor, start, frame.LengthTime));
                    start += frame.LengthTime;
                }
            }

            totalLength = start;
            this.delay = delay;
            Reset(DateTime.UtcNow);
        }

        public void Reset(DateTime utcNow)
        {
            if (loop)
                startTime = utcNow - delay;
            else
                startTime = utcNow + delay;

            lock (colorLock)
            {
                color = null;
            }
        }

        public void Update(DateTime utcNow)
        {
            if (utcNow < startTime) // The loop hasnt started yet
                return;

            TimeSpan elapsedTime = utcNow - startTime;
            if (elapsedTime > totalLength && loop)
            {
                elapsedTime = new TimeSpan(elapsedTime.Ticks % totalLength.Ticks);
            }

            dummyFrame.StartTime = elapsedTime;
            int index = frames.BinarySearch(dummyFrame);
            AnimationFrame currentFrame;
            if (index >= 0) // elapsedTime is equal to the start time at frames[index]
            {
                currentFrame = frames[index];
            }
            else
            {
                index = ~index;
                if(index >= frames.Count) // elapsedTime is after all other frame start times
                {
                    if (loop)
                        currentFrame = frames[0];
                    else
                        currentFrame = frames[frames.Count - 1];
                }
                else if(index == 0) // elapsedTime is before all other frame start times (impossible?)
                {
                    if (loop)
                        currentFrame = frames[frames.Count - 1];
                    else
                        currentFrame = null;
                }
                else // elapsedTime is before the start time at frames[index]
                {
                    currentFrame = frames[index - 1];
                }
            }
            ColorHSV color = currentFrame?.GetColor(elapsedTime);
            lock(colorLock)
            {
                this.color = color;
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
