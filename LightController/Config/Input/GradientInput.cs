using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override void Init()
        {
            if (Colors == null || Colors.Count == 0)
                return;

            List<GradientInputFrame> frames = GetInputFrames();
            GradientInputFrame currentFrame = frames.First();
            int frameIndex = 0;

            int[] fixtures = FixtureIds.EnumerateValues().ToArray();
            for(int i = 0; i < fixtures.Length; i++)
            {
                double percent = i / (double)fixtures.Length;
                while(percent < currentFrame.Location)
                {
                    if (frameIndex == frames.Count - 1)
                        break;
                    frameIndex++;
                    currentFrame = frames[frameIndex];
                }

                int fixtureId = fixtures[i];

                if (currentFrame.Location == percent || frameIndex == frames.Count - 1)
                {
                    colors[fixtureId] = currentFrame.Color;
                }
                else
                {
                    GradientInputFrame nextFrame = frames[frameIndex + 1];
                    double framePercent = (percent - currentFrame.Location) / (nextFrame.Location - currentFrame.Location);
                    if (framePercent < 0 || framePercent > 1)
                        colors[fixtureId] = ColorHSV.Black;
                    else
                        colors[fixtureId] = ColorHSV.Lerp(currentFrame.Color, nextFrame.Color, framePercent);
                }
            }
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
    }
}
