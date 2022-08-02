using LightController.Config.Dmx;
using OpenDMX.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Dmx
{
    public class DmxProcessor
    {
        private List<DmxFixture> fixtures = new List<DmxFixture>();
        private DmxController controller = new DmxController();
        private bool foundDevice = false;

        public DmxProcessor(DmxConfig config)
        {
            foreach (OpenDMX.NET.FTDI.Device device in controller.GetDevices())
            {
                if (string.IsNullOrWhiteSpace(config.DmxDevice) || device.Description == config.DmxDevice)
                {
                    controller.Open(device.DeviceIndex);
                    foundDevice = true;
                    break;
                }
            }

#if !DEBUG
            if (!foundDevice)
                throw new Exception("No DMX interface detected!");
#endif

            Dictionary<string, DmxDeviceProfile> profiles = config.Fixtures.ToDictionary(x => x.Name);
            foreach(DmxDeviceAddress fixtureAddress in config.Addresses)
            {
                DmxDeviceProfile profile = profiles[fixtureAddress.Name];
                for(int i = 0; i < fixtureAddress.Count; i++)
                    fixtures.Add(new DmxFixture(profile, fixtureAddress, fixtures.Count + 1));
            }
        }

        public void SetInputs(IEnumerable<Config.Input.InputBase> inputs)
        {
            foreach (DmxFixture fixture in fixtures)
                fixture.SetInput(inputs);
        }
        
        public void Write()
        {
            if (!foundDevice)
                return;

            foreach (DmxFixture fixture in fixtures)
            {
                DmxFrame frame = fixture.GetFrame();
                controller.SetChannels(frame.StartAddress, frame.Data);
            }
        }
    }
}
