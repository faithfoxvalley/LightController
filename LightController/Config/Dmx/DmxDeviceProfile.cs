using LightController.Dmx;
using System.Collections.Generic;
using System.Linq;
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
                return AddressMap.Select(x => x?.ToString()).ToArray();
            }

            set
            {
                foreach (string color in value)
                {
                    DmxChannel channel  = DmxChannel.Parse(color, AddressMap.Count);
                    if(channel == null)
                    {
                        // TODO: Warn user
                    }
                    AddressMap.Add(channel);
                }
            }
        }

        [YamlIgnore]
        public List<DmxChannel> AddressMap { get; private set; } = new List<DmxChannel>();

    }
}
