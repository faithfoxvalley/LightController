using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace LightController.Config.Dmx
{
    public class DmxConfig
    {
        [YamlMember(Description = "The index of the device to use, starting at 0")]
        public uint DmxDevice { get; set; } = 0;
        [YamlMember(Description = "Do not warn about missing DMX device")]
        public bool DeviceOptional { get; set; }
        [YamlMember(Description = "List of all light fixtures")]
        public List<DmxDeviceProfile> Fixtures { get; set; } = new List<DmxDeviceProfile>();
        [YamlMember(Description = "List of all light fixture addresses")]
        public List<DmxDeviceAddress> Addresses { get; set; } = new List<DmxDeviceAddress>();
    }
}
