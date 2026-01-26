using System.IO.BACnet;

namespace LightController.Bacnet;

public partial class BacnetProcessor
{
    private class BacNode
    {
        public BacnetAddress Address { get; }
        public uint DeviceId { get; }

        public BacNode(BacnetAddress adr, uint deviceId)
        {
            Address = adr;
            DeviceId = deviceId;
        }
    }
}
