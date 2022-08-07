using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Dmx
{
    public class DmxConfig
    {
        public uint DmxDevice { get; set; } = 0;
        public List<DmxDeviceProfile> Fixtures { get; set; } = new List<DmxDeviceProfile>();
        public List<DmxDeviceAddress> Addresses { get; set; } = new List<DmxDeviceAddress>();
    }
}
