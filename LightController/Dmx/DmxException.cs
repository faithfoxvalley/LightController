using FTD2XX_NET;
using System;

namespace LightController.Dmx;

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
