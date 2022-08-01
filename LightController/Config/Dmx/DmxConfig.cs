using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config.Dmx
{
    public class DmxConfig
    {
        public string DmxDevice { get; set; }
        public List<DmxDeviceProfile> Fixtures { get; set; } = new List<DmxDeviceProfile>();
        public List<DmxDeviceAddress> Addresses { get; set; } = new List<DmxDeviceAddress>();
    }
}
