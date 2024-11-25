using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    [YamlTag("!gradient_input")]
    public class GradientInput : InputBase
    {
        private Dictionary<int, ColorHSV> colors = new Dictionary<int, ColorHSV>();

        [YamlMember(Alias = "Colors")]
        public List<GradientInputFrame> Colors { get; set; }

        [YamlMember(Alias = "SpaceEvenly")]
        public bool SpaceEvenly { get; set; }

        [YamlMember]
        public double Scale { get; set; } = 1;

        [YamlIgnore]
        public Iterator ColorIterator => new Iterator(ApplyScale(GetInputFrames()));

        public override void Init()
        {
            if (Colors == null || Colors.Count == 0)
                return;

            if(Colors.Count == 1)
            {
                ColorHSV color = Colors[0].Color;
                foreach(int id in FixtureIds.EnumerateValues())
                    colors[id] = color;
                return;
            }

            Iterator iterator = ColorIterator;

            int[] fixtures = FixtureIds.EnumerateValues().ToArray();
            for(int i = 0; i < fixtures.Length; i++)
            {
                double percent = i / (double)fixtures.Length;
                int fixtureId = fixtures[i];
                colors[fixtureId] = iterator.GetColor(percent);
            }
        }

        private List<GradientInputFrame> ApplyScale(List<GradientInputFrame> frames)
        {
            if (Scale == 1 || Scale <= 0)
                return frames;

            List<GradientInputFrame> result = new List<GradientInputFrame>((int)Math.Ceiling(frames.Count * Scale));
            double gradientLength = 1.0 / Scale;

            for (int s = 0; s < Scale; s++)
            {
                for(int i = 0; i < frames.Count; i++)
                {
                    GradientInputFrame frame = frames[i];
                    GradientInputFrame clone = new GradientInputFrame(frame);
                    double location = (double)((frame.Location * gradientLength) + (s * gradientLength));
                    if (location > 1)
                    {
                        clone.Location = location;
                        result.Add(clone);
                        break;
                    }
                    clone.Location = location;
                    result.Add(clone);
                }
            }
            return result;
        }

        private List<GradientInputFrame> GetInputFrames()
        {
            if(SpaceEvenly)
            {
                if(Colors.Count == 1)
                {
                    Colors[0].Location = 0;
                    return Colors;
                }

                for (int i = 0; i < Colors.Count; i++)
                {
                    GradientInputFrame frame = Colors[i];
                    double percent = i / (double)(Colors.Count - 1);
                    frame.Location = percent;
                }
                return Colors;
            }

            HashSet<double> frameLocations = new HashSet<double>();
            List<GradientInputFrame> frames = new List<GradientInputFrame>();
            foreach (GradientInputFrame frame in Colors.OrderBy(x => x.Location))
            {
                if (frameLocations.Add(frame.Location))
                    frames.Add(frame);
            }
            return frames;
        }

        public override ColorHSV GetColor(int fixtureId)
        {
            if (colors.TryGetValue(fixtureId, out ColorHSV result))
                return result;
            return ColorHSV.Black;
        }

        public override double GetIntensity(int fixtureid, ColorHSV target)
        {
            return target.Value * intensity.GetIntensity(fixtureid);
        }

        public class Iterator
        {
            private List<GradientInputFrame> frames;
            private GradientInputFrame currentFrame;
            private GradientInputFrame nextFrame;
            private int frameIndex;

            public Iterator(List<GradientInputFrame> scaledFrames)
            {
                frames = scaledFrames;
                Reset();
            }

            public void Reset()
            {
                if (frames.Count < 2)
                    return;
                currentFrame = frames[0];
                nextFrame = frames[1];
                frameIndex = 1;
            }

            public ColorHSV GetColor(double percent)
            {
                if (frames.Count == 0)
                    return ColorHSV.Black;

                if (frames.Count == 1)
                    return frames[0].Color;

                while (percent > nextFrame.Location || percent < currentFrame.Location)
                {
                    if (frameIndex == frames.Count - 1)
                        break;
                    frameIndex++;
                    currentFrame = nextFrame;
                    nextFrame = frames[frameIndex];
                }

                if (currentFrame.Location == percent)
                {
                    return currentFrame.Color;
                }
                else if (nextFrame.Location == percent)
                {
                    return nextFrame.Color;
                }
                else
                {
                    double framePercent = (percent - currentFrame.Location) / (nextFrame.Location - currentFrame.Location);
                    if (framePercent < 0 || framePercent > 1)
                        return ColorHSV.Black;
                    else
                        return ColorHSV.Lerp(currentFrame.Color, nextFrame.Color, framePercent);
                }
            }
        }
    }
}
