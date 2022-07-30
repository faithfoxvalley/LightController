using LightController.Config.Input;
using LightController.Config.Output.Dmx;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LightController.Config.Output
{
    [YamlTag("!dmx_output")]
    public class DmxOutput : OutputBase
    {
        public List<DmxDeviceProfile> Devices { get; set; }

        public override void Update()
        {

        }
    }
}
