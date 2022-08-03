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

        private InputIntensity intensity = new InputIntensity();

        protected InputBase() { }

        public InputBase(ValueSet fixtureIds)
        {
            FixtureIds = fixtureIds;
        }

        public virtual void Init()
        {

        }

        public virtual void Start()
        {

        }

        public virtual void Update()
        {

        }

        public virtual void Stop()
        {

        }

        public abstract ColorRGB GetColor(int fixtureId);

        public virtual double GetIntensity(int fixtureid, ColorRGB target)
        {
            return intensity.GetIntensity(target);
        }
    }
}
