using LightController.Config.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config.Output
{
    public abstract class OutputBase
    {
        public int Channel { get; set; }

        public OutputBase() { }

        public OutputBase(int channel)
        {
            Channel = channel;
        }

        public abstract Task WriteOut(InputBase input);
    }
}
