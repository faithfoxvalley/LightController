using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace LightController.Config.Bacnet
{
    public class BacnetConfig
    {
        [YamlMember(Description = "The local ip to bind, or blank for any address")]
        public string BindIp { get; set; }

        [YamlMember(Description = "The port to use for communication, or blank for 0xBAC0/47808")]
        public ushort Port { get; set; } = 0xBAC0;

        [YamlMember]
        public List<BacnetEvent> Events { get; set; } = new List<BacnetEvent>();

    }
}
