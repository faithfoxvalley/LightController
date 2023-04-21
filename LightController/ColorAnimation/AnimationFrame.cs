using LightController.Color;
using System;

namespace LightController.ColorAnimation
{
    public class AnimationFrame : IComparable<AnimationFrame>
    {
        private ColorHSV startColor;
        private ColorHSV endColor;
        private bool equal;
        private TimeSpan startTime;
        private TimeSpan length;

        public TimeSpan StartTime { get => startTime; set => startTime = value; }

        public AnimationFrame(ColorHSV start, ColorHSV end, TimeSpan startTime, TimeSpan length)
        {
            startColor = start;
            endColor = end;
            this.startTime = startTime;
            equal = start == end;
            this.length = length;
        }

        public int CompareTo(AnimationFrame other)
        {
            return startTime.CompareTo(other.startTime);
        }

        public ColorHSV GetColor(TimeSpan elapsedTime)
        {
            if (equal)
                return startColor;

            if (elapsedTime < startTime)
                return ColorHSV.Black;
            elapsedTime -= startTime;
            if (elapsedTime > length)
                return ColorHSV.Black;
            double percent = elapsedTime / length;
            return ColorHSV.Lerp(startColor, endColor, percent);
        }
    }
}
