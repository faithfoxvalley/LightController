using LightController.Color;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using LightController.ColorAnimation;

namespace LightController.Config.Input
{
    [YamlTag("!animated_input")]
    public class AnimatedInput : InputBase
    {
        [YamlMember(Alias = "Colors")]
        public List<AnimatedInputFrame> Colors { get; set; }

        [YamlMember(Alias = "Loop")]
        public bool Loop { get; set; }

        [YamlMember(Alias = "DelayAnimation")]
        public string DelayAnimation
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
        [YamlMember(Alias = "DelayLength")]
        public double DelayLength
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
        private readonly List<AnimationLoop> loops = new List<AnimationLoop>();
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
                    TimeSpan delay = TimeSpan.FromSeconds(animation.GetDelayForSetIndex(i));
                    AnimationLoop loop = new AnimationLoop(Loop, Colors, delay);
                    foreach (int id in overlap.EnumerateValues())
                        loopsByFixture[id] = loops.Count;
                    loops.Add(loop);
                }
                i++;
            }

            AnimationLoop defaultLoop = new AnimationLoop(Loop, Colors, TimeSpan.Zero);
            bool defaultLoopRequired = false;
            foreach(int id in FixtureIds.EnumerateValues())
            {
                if(!loopsByFixture.ContainsKey(id))
                {
                    loopsByFixture[id] = loops.Count;
                    defaultLoopRequired = true;
                }
            }
            if (defaultLoopRequired)
                loops.Add(defaultLoop);
        }

        public override Task StartAsync(MidiNote note)
        {
            DateTime utcNow = ClockTime.UtcNow;
            foreach(AnimationLoop loop in loops)
                loop.Reset(utcNow);
            return Task.CompletedTask;
        }

        public override Task UpdateAsync()
        {
            DateTime utcNow = ClockTime.UtcNow;
            foreach (AnimationLoop loop in loops)
                loop.Update(utcNow);
            return Task.CompletedTask;
        }

        private AnimationLoop GetLoopForFixture(int fixtureId)
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
