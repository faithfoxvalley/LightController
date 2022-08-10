using LightController.Config.Dmx;
using OpenDMX.NET;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace LightController.Dmx
{
    public class DmxProcessor
    {
        private List<DmxFixture> fixtures = new List<DmxFixture>();
        private DmxController controller = new DmxController();
        private bool foundDevice = false;

        public DmxProcessor(DmxConfig config)
        {


            while (!OpenDevice(config.DmxDevice))
            {
#if DEBUG
                break;
#else
                var result = MessageBox.Show("DMX Device not found. Press OK to try again or Cancel to exit.", 
                    "Light Controller", MessageBoxButton.OKCancel);
                if(result == MessageBoxResult.Cancel)
                {
                    Application.Current.Shutdown();
                    return;
                }
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

        private bool OpenDevice(uint deviceIndex)
        {
            var devices = controller.GetDevices();
            if (deviceIndex < devices.Length)
            {
                try
                {
                    controller.Open(deviceIndex);
                    return true;
                }
                catch { }
            }
            return false;
        }

        /// <summary>
        /// Turns off all fixtures
        /// </summary>
        public void TurnOff()
        {
            if (!controller.IsOpen)
                return;

            foreach (DmxFixture fixture in fixtures)
            {
                DmxFrame frame = fixture.GetOffFrame();
                controller.SetChannels(frame.StartAddress, frame.Data);
            }

            controller.WriteData();
        }

        public void SetInputs(IEnumerable<Config.Input.InputBase> inputs)
        {
            foreach (DmxFixture fixture in fixtures)
                fixture.SetInput(inputs);
        }
        
        public void Write()
        {
            if (!controller.IsOpen)
                return;

            foreach (DmxFixture fixture in fixtures)
            {
                DmxFrame frame = fixture.GetFrame();
                controller.SetChannels(frame.StartAddress, frame.Data);
            }

            controller.WriteData();
        }
    }
}
