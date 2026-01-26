using LightController.Color;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Input;

public abstract class InputBase
{
    [YamlIgnore]
    public ValueSet FixtureIds { get; set; }

    [YamlMember(Alias = "FixtureIds")]
    public string FixtureRange
    {
        get
        {
            return FixtureIds.ToString();
        }
        set
        {
            if (value == null)
                FixtureIds = new ValueSet();
            else
                FixtureIds = new ValueSet(value);
        }
    }

    [YamlMember(Alias = "Intensity")]
    public string IntensityMode
    {
        get
        {
            return intensity.ToString();
        }
        set
        {
            intensity = InputIntensity.Parse(value, 1);
        }
    }

    protected InputIntensity intensity = new InputIntensity(1);

    protected InputBase() { }

    public InputBase(ValueSet fixtureIds)
    {
        FixtureIds = fixtureIds;
    }

    public virtual void Init()
    {

    }

    public virtual Task StartAsync(Midi.MidiNote note, CancellationToken cancelToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task UpdateAsync(CancellationToken cancelToken)
    {
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancelToken)
    {
        return Task.CompletedTask;
    }

    public abstract ColorHSV GetColor(int fixtureId);

    public abstract double GetIntensity(int fixtureid, ColorHSV target);
}
