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

        [YamlMember(Alias = "Animation")]
        public string AnimationValue
        {
            get
            {
                return animation?.ToString();
            }
            set
            {
                animation = new Animation(value);
            }
        }
        [YamlMember(Alias = "AnimationLength")]
        public double AnimationLength
        {
            get
            {
                return animation.Length;
            }
            set
            {
                animation.Length = value;
            }
        }

        private Animation animation = new Animation();
        private readonly List<AnimatedInputLoop> loops = new List<AnimatedInputLoop>();
        private readonly Dictionary<int, int> loopsByFixture = new Dictionary<int, int>();

        public AnimatedInput() { }

        public override void Init()
        {
            if (Colors == null)
                Colors = new List<AnimatedInputFrame>();

            animation.LengthIncludesLastSet = false;
            int i = 0;
            foreach(ValueSet set in animation.AnimationOrder)
            {
                if (set.GetOverlap(FixtureIds, out ValueSet overlap))
                {
                    double delay = animation.GetDelayForSetIndex(i);
                    AnimatedInputLoop loop = new AnimatedInputLoop(Loop, Colors, delay);
                    foreach (int id in overlap.EnumerateValues())
                        loopsByFixture[id] = loops.Count;
                    loops.Add(loop);
                }
                i++;
            }
        }

        public override Task StartAsync(MidiNote note)
        {
            foreach(AnimatedInputLoop loop in loops)
                loop.Reset();
            return Task.CompletedTask;
        }

        public override Task UpdateAsync()
        {
            foreach (AnimatedInputLoop loop in loops)
                loop.Update();
            return Task.CompletedTask;
        }

        private AnimatedInputLoop GetLoopForFixture(int fixtureId)
        {
            return loops[loopsByFixture[fixtureId]];
        }

        public override ColorHSV GetColor(int fixtureId)
        {
            return GetLoopForFixture(fixtureId).GetColor();
        }

        public override double GetIntensity(int fixtureid, ColorHSV target)
        {
            return (intensity.Value ?? 1) * GetLoopForFixture(fixtureid).GetIntensity();

        }
    }
}
