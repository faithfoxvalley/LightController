using LightController.Config.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Output
{
    public abstract class OutputBase
    {
        [YamlIgnore]
        public ValueSet Channels { get; set; }

        [YamlMember(Alias = "Channels")]
        public string ChannelRange
        {
            get => Channels.ToString();
            set => Channels = new ValueSet(value);
        }

        protected List<OutputMapping> inputs;

        public OutputBase() { }

        public OutputBase(ValueSet channels)
        {
            Channels = channels;
        }

        public void AssignInputs(IEnumerable<InputBase> inputs)
        {
            this.inputs = new List<OutputMapping>();
            foreach(InputBase input in inputs)
            {
                if (input.Channels.GetOverlap(Channels, out ValueSet overlap))
                    this.inputs.Add(new OutputMapping(input, overlap));
            }
        }

        public abstract void Update();


        protected class OutputMapping
        {
            public OutputMapping(InputBase input, ValueSet channels)
            {
                Input = input;
                Channels = channels;
            }

            public InputBase Input { get; set; }
            public ValueSet Channels { get; set; }
        }
    }
}
