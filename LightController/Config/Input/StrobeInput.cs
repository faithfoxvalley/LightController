using LightController.Color;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    [YamlTag("!strobe_input")]
    public class StrobeInput : InputBase
    {
        public StrobeState On { get; set; } = new StrobeState();
        public StrobeState Off { get; set; } = new StrobeState();

        [YamlMember(Alias = "animation", ApplyNamingConventions = false)]
        public string Groups
        {
            get
            {
                return groups?.ToString();
            }
            set
            {
                groups = new Animation(value);
            }
        }

        private Animation groups = new Animation();
        private readonly Random random = new Random();
        private readonly List<StrobeLoop> loops = new List<StrobeLoop>();
        private readonly Dictionary<int, StrobeLoop> fixtureLoops = new Dictionary<int, StrobeLoop>();

        public StrobeInput() { }

        public override void Init()
        {
            if(On == null)
                On = new StrobeState();
            On.Init();
            if(Off == null)
                Off = new StrobeState();
            Off.Init();

            StrobeLoop defaultLoop = new StrobeLoop(On, Off, random);
            loops.Add(defaultLoop);
            foreach(int fixtureId in FixtureIds.EnumerateValues())
                fixtureLoops[fixtureId] = defaultLoop;
            foreach(ValueSet set in groups.AnimationOrder)
            {
                StrobeLoop loop = new StrobeLoop(On, Off, random);
                loops.Add(loop);
                foreach (int fixtureId in set.EnumerateValues())
                    fixtureLoops[fixtureId] = loop;
            }
        }

        public override Task StartAsync(MidiNote note, CancellationToken cancelToken)
        {
            DateTime now = ClockTime.UtcNow;
            foreach (StrobeLoop loop in loops)
                loop.Start(now);
            return Task.CompletedTask;
        }

        public override Task UpdateAsync(CancellationToken cancelToken)
        {
            DateTime now = ClockTime.UtcNow;
            foreach (StrobeLoop loop in loops)
                loop.Update(now);
            return Task.CompletedTask;
        }

        private StrobeLoop GetLoopForFixture(int fixtureId)
        {
            return fixtureLoops[fixtureId];
        }

        public override ColorHSV GetColor(int fixtureId)
        {
            return GetLoopForFixture(fixtureId).Color;
        }

        public override double GetIntensity(int fixtureid, ColorHSV target)
        {
            return intensity.GetIntensity(fixtureid) * target.Value;
        }

        public class StrobeState
        {
            [YamlMember(Alias = "hsv", ApplyNamingConventions = false)]
            public SerializableColorHSV HSV { get; set; } = new SerializableColorHSV();

            public double MinLength { get; set; }

            public double MaxLength { get; set; }

            private TimeSpan lengthRange;
            private TimeSpan minLength;

            public StrobeState() { }

            public void Init()
            {
                if(HSV == null)
                    HSV = new SerializableColorHSV();
                if (MaxLength < MinLength)
                    MaxLength = MinLength;
                minLength = TimeSpan.FromSeconds(MinLength);
                lengthRange = TimeSpan.FromSeconds(MaxLength) - minLength;
            }

            public TimeSpan GetLength(Random random)
            {
                if (lengthRange.Ticks == 0)
                    return minLength;
                return minLength + (random.NextDouble() * lengthRange);
            }
        }

        private class StrobeLoop
        {
            private readonly StrobeState on;
            private readonly StrobeState off;
            private readonly Random random;
            private DateTime nextState;
            private bool isOn = true;

            public ColorHSV Color { get; private set; }

            public StrobeLoop(StrobeState on, StrobeState off, Random random)
            {
                this.on = on;
                this.off = off;
                this.random = random;
            }

            public void Start(DateTime now)
            {
                isOn = true;
                nextState = now + on.GetLength(random);
            }

            public void Update(DateTime now)
            {
                if (now >= nextState)
                {
                    isOn = !isOn;
                    if (isOn)
                        nextState = now + on.GetLength(random);
                    else
                        nextState = now + off.GetLength(random);
                }

                if (isOn)
                    Color = on.HSV.Color;
                else
                    Color = off.HSV.Color;
            }
        }
    }
}
