using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Dmx
{
    public class DmxException : Exception
    {
        public FTDI.FT_STATUS Status { get; }

        public DmxException() : base()
        {
            
        }

        public DmxException(string msg) : base(msg) 
        {
            
        }

        public DmxException(string msg, FTDI.FT_STATUS status) : base(msg) 
        {
            Status = status;
        }
    }
}
