using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Dmx
{
    internal class NullDmxController : IDmxController
    {
        public bool IsOpen => false;

        public void SetChannel(int channel, byte value)
        {

        }

        public void SetChannels(int startAddress, byte[] data)
        {

        }

        public void WriteData()
        {

        }

        public void WriteDebugInfo(StringBuilder sb, int columns)
        {

        }
    }
}
