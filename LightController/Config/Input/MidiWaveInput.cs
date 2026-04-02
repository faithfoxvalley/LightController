using LightController.Color;
using LightController.ColorAnimation;
using LightController.Config.Animation;
using LightController.Midi;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Input;

[YamlTag("!midiwave_input")]
internal class MidiWaveInput : InputBase
{
    public string MidiDevice { get; set; }
    public MidiNote MidiNote { get; set; }
    public SerializableColorHSV Hsv { get; set; }
    public double RisingTime { get; set; }
    public double OnTime { get; set; }
    public double FallingTime { get; set; }

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
    public double DelayLength { get; set; }
    public double DelayScale { get; set; }

    private double WaveLength => RisingTime + OnTime + FallingTime;
    private TimeSpan rising, on, falling;
    private TimeSpan perSegmentDelay = TimeSpan.Zero;
    private AnimationOrder animation = new AnimationOrder();
    private readonly ConcurrentQueue<Wave> waves = new ConcurrentQueue<Wave>();
    private readonly ConcurrentDictionary<int, double> fixtureIntensity = new ConcurrentDictionary<int, double>();
    private MidiInput midi;

    public override void Init()
    {
        if (string.IsNullOrEmpty(MidiDevice) || !MainWindow.Instance.Midi.TryGetInput(MidiDevice, out midi))
            Log.Error($"Unable to find midi device for drum wave input!");

        if(double.IsFinite(RisingTime) && RisingTime > 0)
            rising = TimeSpan.FromSeconds(RisingTime);
        else
            rising = TimeSpan.Zero;

        if(double.IsFinite(OnTime) && OnTime > 0)
            on = TimeSpan.FromSeconds(OnTime);
        else
            on = TimeSpan.FromSeconds(1);

        if(double.IsFinite(FallingTime) && FallingTime > 0)
            falling = TimeSpan.FromSeconds(FallingTime);
        else
            falling = TimeSpan.Zero;

        if (animation.Count > 1)
        {
            double delayLength = DelayLength;
            if (delayLength <= 0 && DelayScale > 0)
                delayLength = WaveLength * DelayScale;
            if (delayLength > 0)
                perSegmentDelay = TimeSpan.FromSeconds(delayLength / (animation.Count - 1));
        }

    }

    public override Task StartAsync(MidiNote note, CancellationToken cancelToken)
    {
        midi?.NoteEvent += OnMidiNote;
        return Task.CompletedTask;
    }

    public override Task StopAsync(CancellationToken cancelToken)
    {
        midi?.NoteEvent -= OnMidiNote;
        return Task.CompletedTask;
    }

    private void OnMidiNote(MidiNote note)
    {
        if (note == MidiNote)
            waves.Enqueue(new Wave(ClockTime.UtcNow, rising, on, falling));
    }

    public override Task UpdateAsync(CancellationToken cancelToken)
    {
        DateTime now = ClockTime.UtcNow;

        foreach (ValueSet set in animation.Values)
        {
            foreach (Wave wave in waves)
            {
                double intensity = wave.GetIntensity(now);
                foreach (int id in set.EnumerateValues())
                    fixtureIntensity[id] = intensity;
            }
            now += perSegmentDelay;
        }

        while (waves.TryPeek(out Wave firstWave) && firstWave.IsOver(now))
            waves.TryDequeue(out _);

        return Task.CompletedTask;
    }


    public override ColorHSV GetColor(int fixtureId)
    {
        return Hsv.Color;
    }

    public override double GetIntensity(int fixtureId, ColorHSV target)
    {
        if (!fixtureIntensity.TryGetValue(fixtureId, out double intensity))
            return 0;
        return this.intensity.GetIntensity(fixtureId) * intensity;
    }

    private class Wave
    {
        public Wave(DateTime utcNow, TimeSpan rising, TimeSpan length, TimeSpan falling)
        {
            this.rising = rising;
            this.length = length;
            this.falling = falling;

            starting = utcNow;
            on = starting + rising;
            ending = on + length;
            off = ending + falling;
        }

        private readonly TimeSpan rising;
        private readonly TimeSpan length;
        private readonly TimeSpan falling;

        private readonly DateTime starting;
        private readonly DateTime on;
        private readonly DateTime ending;
        private readonly DateTime off;

        public bool IsOver(DateTime utcNow)
        {
            return utcNow >= off;
        }

        public double GetIntensity(DateTime utcNow)
        {
            // after end or before beginning
            if (utcNow >= off || utcNow < starting)
                return 0;

            // falling state
            if (utcNow > ending)
            {
                TimeSpan length = utcNow - ending;
                return length / falling;
            }
            
            // on state
            if (utcNow >= on)
                return 1;

            // rising state
            {
                TimeSpan length = utcNow - starting;
                return length / rising;
            }
        }

    }
}
