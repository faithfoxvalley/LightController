using LightController.Color;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    public abstract class InputBase
    {
        [YamlIgnore]
        public ValueSet FixtureIds { get; set; }

        [YamlMember(Alias = "FixtureIds")]
        public string FixtureRange
        {
            get => FixtureIds.ToString();
            set => FixtureIds = new ValueSet(value);
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
                intensity = InputIntensity.Parse(value);
            }
        }

        protected InputIntensity intensity = new InputIntensity();

        protected InputBase() { }

        public InputBase(ValueSet fixtureIds)
        {
            FixtureIds = fixtureIds;
        }

        public virtual void Init()
        {

        }

        public virtual Task StartAsync(Midi.MidiNote note)
        {
            return Task.CompletedTask;
        }

        public virtual Task UpdateAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public abstract ColorHSV GetColor(int fixtureId);

        public virtual double GetIntensity(int fixtureid, ColorHSV target)
        {
            return intensity.GetIntensity(target);
        }
    }
}
