﻿using LightController.Color;
using System.Collections.Generic;
using System.Linq;
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
        public int Scale { get; set; } = 1;

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



            List<GradientInputFrame> frames = ApplyScale(GetInputFrames());
            GradientInputFrame currentFrame = frames[0];
            GradientInputFrame nextFrame = frames[1];
            int frameIndex = 1;

            int[] fixtures = FixtureIds.EnumerateValues().ToArray();
            for(int i = 0; i < fixtures.Length; i++)
            {
                double percent = i / (double)fixtures.Length;

                while (percent > nextFrame.Location || percent < currentFrame.Location)
                {
                    if (frameIndex == frames.Count - 1)
                        break;
                    frameIndex++;
                    currentFrame = nextFrame;
                    nextFrame = frames[frameIndex];
                }
                int fixtureId = fixtures[i];

                if (currentFrame.Location == percent)
                {
                    colors[fixtureId] = currentFrame.Color;
                }
                else if (nextFrame.Location == percent)
                {
                    colors[fixtureId] = nextFrame.Color;
                }
                else
                {
                    double framePercent = (percent - currentFrame.Location) / (nextFrame.Location - currentFrame.Location);
                    if (framePercent < 0 || framePercent > 1)
                        colors[fixtureId] = ColorHSV.Black;
                    else
                        colors[fixtureId] = ColorHSV.Lerp(currentFrame.Color, nextFrame.Color, framePercent);
                }
            }
        }

        private List<GradientInputFrame> ApplyScale(List<GradientInputFrame> frames)
        {
            if (Scale <= 1)
                return frames;

            List<GradientInputFrame> result = new List<GradientInputFrame>(frames.Count * Scale);
            double gradientLength = 1.0 / Scale;

            for (int s = 0; s < Scale; s++)
            {
                for(int i = 0; i < frames.Count; i++)
                {
                    GradientInputFrame frame = frames[i];
                    GradientInputFrame clone = new GradientInputFrame(frame);
                    clone.Location = (double)((frame.Location * gradientLength) + (s * gradientLength));
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
    }
}
