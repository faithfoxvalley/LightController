using LightController.Midi;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace LightController.Config.Bacnet
{
    public class BacnetEvent
    {
        [YamlMember]
        public string Name { get; set; }

        [YamlMember]
        public MidiNote MidiNote { get; set; }

        [YamlMember]
        public List<BacnetProperty> Properties { get; set; } = new List<BacnetProperty>();

        public void Init()
        {
            if (Properties == null)
                Properties = new List<BacnetProperty>();

            foreach (BacnetProperty prop in Properties)
                prop.Init();
        }

        public override string ToString()
        {
            if (MidiNote == null)
                return Name;
            return $"{MidiNote.Channel},{MidiNote.Note} - {Name}";
        }
    }
}
