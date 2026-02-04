using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace LightController.Config.Dmx;

public class DmxConfig
{
    [YamlMember(Description = "The serial numbers of devices to use for DMX output")]
    public List<string> Interfaces { get; set; }

    [YamlMember(Description = "Do not warn about missing DMX device")]
    public bool DeviceOptional { get; set; }

    [YamlMember(Description = "List of all light fixtures")]
    public List<DmxDeviceProfile> Fixtures { get; set; } = new List<DmxDeviceProfile>();

    [YamlMember(Description = "List of all light fixture addresses")]
    public List<DmxDeviceAddress> Addresses { get; set; } = new List<DmxDeviceAddress>();
}
