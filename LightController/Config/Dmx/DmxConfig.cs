using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace LightController.Config.Dmx
{
    public class DmxConfig
    {
        [YamlMember(Description = "The index of the device to use, starting at 0")]
        public uint DmxDevice { get; set; } = 0;
        [YamlMember(Description = "List of all light fixtures")]
        public List<DmxDeviceProfile> Fixtures { get; set; } = new List<DmxDeviceProfile>();
        [YamlMember(Description = "List of all light fixture addresses")]
        public List<DmxDeviceAddress> Addresses { get; set; } = new List<DmxDeviceAddress>();
        [YamlMember(Description = "Whether Art-Net is enabled")]
        public bool ArtNet { get; set; }
        [YamlMember(Description = "IP address of the art-net interface to use, or empty")]
        public string ArtNetAddress { get; set; }
    }
}
