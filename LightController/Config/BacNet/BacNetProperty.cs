using LightController.BacNet;
using OpenDMX.NET.FTDI;
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.BacNet
{
    public class BacNetProperty
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
        public BacnetObjectId BacnetId { get; private set; }

        [YamlIgnore]
        public BacnetValue BacnetValue { get; private set; }

        public void Init()
        {
            BacnetObjectTypes type;
            if(OutputType)
            {
                if (AnalogType)
                    type = BacnetObjectTypes.OBJECT_ANALOG_OUTPUT;
                else
                    type = BacnetObjectTypes.OBJECT_BINARY_OUTPUT;
                BacnetValue = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL, (float)Value);
            }
            else
            {
                if (AnalogType)
                    type = BacnetObjectTypes.OBJECT_ANALOG_VALUE;
                else
                    type = BacnetObjectTypes.OBJECT_BINARY_VALUE;
                BacnetValue = new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, (uint)Value);
            }
            BacnetId = new BacnetObjectId(type, PropertyId);
        }
    }
}
