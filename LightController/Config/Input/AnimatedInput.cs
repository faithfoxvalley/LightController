using LightController.Color;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using LightController.ColorAnimation;
using System.Threading;
using LightController.Config.Animation;
using System.Linq;

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
                animation = new AnimationOrder(value);
            }
        }
        [YamlMember(Alias = "DelayLength")]
        public double DelayLength { get; set; }
        [YamlMember]
        public double DelayCount { get; set; }

        private AnimationOrder animation = new AnimationOrder();
        private readonly List<AnimationLoop> loops = new List<AnimationLoop>();
        private readonly Dictionary<int, int> loopsByFixture = new Dictionary<int, int>();

        public AnimatedInput() { }

        public override void Init()
        {
            if (Colors == null)
                Colors = new List<AnimatedInputFrame>();

            double perFixtureDelay = 0;
            if(animation.Count > 1)
            {
                double delayLength = DelayLength;
                if(delayLength <= 0 && DelayCount > 0)
                    delayLength = Colors.Sum(x => x.Length) * DelayCount;
                if(delayLength > 0)
                    perFixtureDelay = delayLength / (animation.Count - 1);
            }

            int i = 0;
            foreach(ValueSet set in animation.Values)
            {
                if (set.GetOverlap(FixtureIds, out ValueSet overlap))
                {
                    TimeSpan delay = TimeSpan.FromSeconds(perFixtureDelay * i);
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

        public override Task StartAsync(MidiNote note, CancellationToken cancelToken)
        {
            DateTime utcNow = ClockTime.UtcNow;
            foreach(AnimationLoop loop in loops)
                loop.Reset(utcNow);
            return Task.CompletedTask;
        }

        public override Task UpdateAsync(CancellationToken cancelToken)
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

        public override double GetIntensity(int fixtureId, ColorHSV target)
        {
            return (intensity.GetIntensity(fixtureId)) * GetLoopForFixture(fixtureId).GetIntensity();

        }
    }
}
