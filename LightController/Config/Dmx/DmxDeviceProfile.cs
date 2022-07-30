using LightController.Color;
using LightController.Dmx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace LightController.Config.Dmx
{
    /// <summary>
    /// Contains device profile information
    /// </summary>
    public class DmxDeviceProfile
    {
        public string Name { get; set; }

        public int DmxLength { get; set; }

        [YamlMember(Alias = "AddressMap")]
        public string[] AddressMapStrings
        {
            get
            {
                return AddressMap.Select(x => x.ToString().ToLowerInvariant()).ToArray();
            }

            set
            {
                foreach (string color in value)
                {
                    if (Enum.TryParse(color, out DmxChannel result))
                    {
                        AddressMap.Add(result);
                        FlatAddressMap |= result;
                    }
                }
            }
        }
        [YamlIgnore]
        public List<DmxChannel> AddressMap { get; private set; } = new List<DmxChannel>();
        [YamlIgnore]
        public DmxChannel FlatAddressMap { get; private set; } = DmxChannel.Unknown;


    }
}
