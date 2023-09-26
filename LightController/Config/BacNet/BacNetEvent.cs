using LightController.BacNet;
using LightController.Midi;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace LightController.Config.BacNet
{
    public class BacNetEvent
    {
        [YamlMember]
        public string Name { get; set; }

        [YamlMember]
        public MidiNote MidiNote { get; set; }

        [YamlMember]
        public List<BacNetProperty> Properties { get; set; } = new List<BacNetProperty>();

        public void Init()
        {
            if (Properties == null)
                Properties = new List<BacNetProperty>();

            foreach (BacNetProperty prop in Properties)
                prop.Init();
        }
    }
}
