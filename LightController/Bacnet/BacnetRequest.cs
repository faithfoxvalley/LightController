using System.IO.BACnet;

namespace LightController.Bacnet;

public class BacnetRequest
{

    public BacnetEndpoint Endpoint { get; }
    public BacnetValue Value { get; }

    public BacnetRequest(BacnetEndpoint endpoint, BacnetValue value)
    {
        Endpoint = endpoint;
        Value = value;
    }
}
