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

        public abstract Colourful.RGBColor GetColor();
    }
}
