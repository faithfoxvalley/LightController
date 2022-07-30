using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config.Dmx
{
    public class DmxDeviceAddress
    {
        public string Name { get; set; }
        public int StartAddress { get; set; }
        public int Count { get; set; }
    }
}
