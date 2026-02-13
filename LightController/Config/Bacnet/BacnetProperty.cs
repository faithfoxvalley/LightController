using LightController.Bacnet;
using System.IO.BACnet;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Bacnet;

public class BacnetProperty
{
    [YamlMember(Alias = "Device")]
    public uint DeviceId { get; set; }

    [YamlMember]
    public bool AnalogType { get; set; }

    [YamlMember]
    public bool OutputType { get; set; }

    [YamlMember(Alias = "Id")]
    public uint PropertyId { get; set; }

    [YamlMember]
    public double Value { get; set; }

    [YamlIgnore]
    public BacnetEndpoint Endpoint { get; private set; }

    [YamlIgnore]
    public BacnetRequest ValueRequest { get; private set; }

    public void Init()
    {
        BacnetObjectTypes type;
        BacnetValue val;

        if(AnalogType)
        {
            if (OutputType)
                type = BacnetObjectTypes.OBJECT_ANALOG_OUTPUT;
            else
                type = BacnetObjectTypes.OBJECT_ANALOG_VALUE;
            val = new BacnetValue((float)Value);
        }
        else
        {
            if (OutputType)
                type = BacnetObjectTypes.OBJECT_BINARY_OUTPUT;
            else
                type = BacnetObjectTypes.OBJECT_BINARY_VALUE;
            val = new BacnetValue(Value == 1);
        }
        Endpoint = new BacnetEndpoint(DeviceId, type, PropertyId);
        ValueRequest = new BacnetRequest(Endpoint, val);
    }

}
