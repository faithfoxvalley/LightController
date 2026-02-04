using YamlDotNet.Serialization;

namespace LightController.Config.Dmx;

public class DmxInterface
{
    [YamlMember]
    public string SerialNumber { get; set; }
    [YamlMember]
    public int Universe { get; set; } = 1;
}