using LightController.Bacnet;
using System;
using System.IO.BACnet;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Bacnet;

public class BacnetProperty
{
    private bool verified = false;

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

    public async Task FixValueType(BacnetClient bacnet, BacnetAddress deviceAddress)
    {
        if (verified)
            return;
        verified = true;

        try
        {
            var queryResult = await bacnet.ReadPropertyAsync(deviceAddress, Endpoint.ObjectId, BacnetPropertyIds.PROP_PRESENT_VALUE);
            if(queryResult.Count == 0)
            {
                Log.Warn("[Bacnet] Unable to verify property value " + Endpoint);
                return;
            }
            BacnetValue currentValue = queryResult[0];
            object desiredValueData;
            switch (currentValue.Tag)
            {
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                    desiredValueData = Value == 1;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    desiredValueData = (uint)Value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_SIGNED_INT:
                    desiredValueData = (int)Value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                    desiredValueData = (float)Value;
                    break;
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_DOUBLE:
                    desiredValueData = (double)Value;
                    break;
                default:
                    Log.Warn($"[Bacnet] Unable to verify property value {Endpoint}, property has unsupported type of {currentValue.Tag}");
                    return;
            }
            ValueRequest = new BacnetRequest(Endpoint, new BacnetValue(currentValue.Tag, desiredValueData));
        }
        catch (Exception e)
        {
            Log.Warn($"[Bacnet] Exception while attempting to verify property value {Endpoint}: {e}");
        }
    }
}
