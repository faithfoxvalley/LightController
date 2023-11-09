using LightController.Bacnet;
using System.IO.BACnet;
using YamlDotNet.Serialization;

namespace LightController.Config.Bacnet
{
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
            if(OutputType)
            {
                if (AnalogType)
                    type = BacnetObjectTypes.OBJECT_ANALOG_OUTPUT;
                else
                    type = BacnetObjectTypes.OBJECT_BINARY_OUTPUT;
                val = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, (float)Value);
            }
            else
            {
                if (AnalogType)
                    type = BacnetObjectTypes.OBJECT_ANALOG_VALUE;
                else
                    type = BacnetObjectTypes.OBJECT_BINARY_VALUE;
                val = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, (uint)Value);
            }
            Endpoint = new BacnetEndpoint(DeviceId, type, PropertyId);
            ValueRequest = new BacnetRequest(Endpoint, val);
        }
    }
}
