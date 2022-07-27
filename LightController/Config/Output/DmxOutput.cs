using LightController.Config.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config.Output
{
    [YamlTag("!dmx_output")]
    public class DmxOutput : OutputBase
    {
        public int Address { get; set; }

        public override Task WriteOut(InputBase input)
        {
            throw new NotImplementedException();
        }
    }
}
