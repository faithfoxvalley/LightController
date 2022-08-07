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
            var devices = controller.GetDevices();
            if(config.DmxDevice < devices.Length)
            {
                controller.Open(config.DmxDevice);
                foundDevice = true;
            }

            if(!foundDevice)
            {
                LogFile.Error("No DMX interface detected!");
#if !DEBUG
                throw new Exception("No DMX interface detected!");
#endif
            }

            Dictionary<string, DmxDeviceProfile> profiles = config.Fixtures.ToDictionary(x => x.Name);
            foreach(DmxDeviceAddress fixtureAddress in config.Addresses)
            {
                DmxDeviceProfile profile = profiles[fixtureAddress.Name];
                int address = fixtureAddress.StartAddress;
                for (int i = 0; i < fixtureAddress.Count; i++)
                {
                    fixtures.Add(new DmxFixture(profile, address, fixtures.Count + 1));
                    address += profile.DmxLength;
                }
            }
        }

        /// <summary>
        /// Turns off all fixtures
        /// </summary>
        public void TurnOff()
        {
            foreach (DmxFixture fixture in fixtures)
            {
                DmxFrame frame = fixture.GetOffFrame();
                controller.SetChannels(frame.StartAddress, frame.Data);
            }
        }

        public void SetInputs(IEnumerable<Config.Input.InputBase> inputs)
        {
            foreach (DmxFixture fixture in fixtures)
                fixture.SetInput(inputs);
        }
        
        public void Write()
        {
            foreach (DmxFixture fixture in fixtures)
            {
                DmxFrame frame = fixture.GetFrame();
                controller.SetChannels(frame.StartAddress, frame.Data);
            }
        }
    }
}
