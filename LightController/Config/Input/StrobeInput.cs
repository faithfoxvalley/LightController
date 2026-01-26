using LightController.Color;
using LightController.Config.Animation;
using LightController.Midi;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Input;

[YamlTag("!strobe_input")]
public class StrobeInput : InputBase
{
    public StrobeState On { get; set; } = new StrobeState();
    public StrobeState Off { get; set; } = new StrobeState();

    [YamlMember(Alias = "animation", ApplyNamingConventions = false)]
    public string Animation
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

    private AnimationOrder animation = new AnimationOrder();
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
        foreach(ValueSet set in animation.Values)
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

    public override double GetIntensity(int fixtureId, ColorHSV target)
    {
        return intensity.GetIntensity(fixtureId) * GetLoopForFixture(fixtureId).Intensity;
    }

    public class StrobeState
    {
        [YamlMember(Alias = "hsv", ApplyNamingConventions = false)]
        public SerializableColorHSV HSV { get; set; } = new SerializableColorHSV();

        public double MinLength { get; set; }

        public double MaxLength { get; set; }

        private Percent minIntensity = new Percent(1);
        public string MinIntensity
        {
            get => minIntensity.ToString();
            set => minIntensity = Percent.Parse(value, 1);
        }

        private Percent maxIntensity = new Percent(1);
        public string MaxIntensity
        {
            get => maxIntensity.ToString();
            set => maxIntensity = Percent.Parse(value, 1);
        }

        private TimeSpan lengthRange;
        private TimeSpan minLength;
        private double intensityRange;

        public StrobeState() { }

        public void Init()
        {
            if(HSV == null)
                HSV = new SerializableColorHSV();
            if (MaxLength < MinLength)
                MaxLength = MinLength;
            minLength = TimeSpan.FromSeconds(MinLength);
            lengthRange = TimeSpan.FromSeconds(MaxLength) - minLength;

            if(maxIntensity.Value > minIntensity.Value)
                intensityRange = maxIntensity.Value - minIntensity.Value;
        }

        public TimeSpan GetLength(Random random)
        {
            if (lengthRange.Ticks == 0)
                return minLength;
            return minLength + (random.NextDouble() * lengthRange);
        }

        public double GetIntensity(Random random)
        {
            if (maxIntensity.Value == 0)
                return minIntensity.Value;
            return minIntensity.Value + (random.NextDouble() * intensityRange);
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
        public double Intensity { get; private set; }

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
            Color = on.HSV.Color;
            Intensity = on.GetIntensity(random);
        }

        public void Update(DateTime now)
        {
            bool isNew = now >= nextState;
            if (!isNew)
                return;

            isOn = !isOn;

            StrobeState state;
            if (isOn)
                state = on;
            else
                state = off;

            nextState = now + state.GetLength(random);
            Intensity = state.GetIntensity(random);
            Color = state.HSV.Color;
        }
    }
}
