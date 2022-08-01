using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    public abstract class InputBase
    {
        [YamlIgnore]
        public ValueSet Channels { get; set; }

        [YamlMember(Alias = "Channels")]
        public string ChannelRange
        {
            get => Channels.ToString();
            set => Channels = new ValueSet(value);
        }

        protected InputBase() { }

        public InputBase(ValueSet channels)
        {
            Channels = channels;
        }

        public virtual void Init()
        {

        }

        public virtual void Start()
        {

        }

        public virtual void Stop()
        {

        }
    }
}
