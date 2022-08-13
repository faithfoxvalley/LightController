using LightController.Config.Dmx;
using OpenDMX.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace LightController.Dmx
{
    public class DmxProcessor
    {
        private bool debug;
        private List<DmxFixture> fixtures = new List<DmxFixture>();
        private DmxController controller = new DmxController();

        public DmxProcessor(DmxConfig config, double mixLength)
        {
            if(config == null)
            {
                ErrorBox.Show("No DMX settings found, please check your config.");
                return;
            }

            while (!OpenDevice(config.DmxDevice))
            {
#if DEBUG
                break;
#else
                ErrorBox.ExitOnCancel("DMX Device not found. Press OK to try again or Cancel to exit."); 
#endif
            }

            if (double.IsNaN(mixLength) || double.IsInfinity(mixLength))
                mixLength = 0;

            Dictionary<string, DmxDeviceProfile> profiles = config.Fixtures.ToDictionary(x => x.Name);
            foreach(DmxDeviceAddress fixtureAddress in config.Addresses)
            {
                DmxDeviceProfile profile = profiles[fixtureAddress.Name];
                int address = fixtureAddress.StartAddress;
                for (int i = 0; i < fixtureAddress.Count; i++)
                {
                    fixtures.Add(new DmxFixture(profile, address, fixtures.Count + 1, mixLength));
                    address += profile.DmxLength;
                }
            }
        }

        public void AppendToListbox(System.Windows.Controls.ListBox list)
        {
            list.Items.Clear();
            foreach (DmxFixture fixture in fixtures)
                list.Items.Add(fixture);
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
            foreach (DmxFixture fixture in fixtures)
                fixture.TurnOff();
        }

        public void SetInputs(IEnumerable<Config.Input.InputBase> inputs)
        {
            foreach (DmxFixture fixture in fixtures)
                fixture.SetInput(inputs);
        }
        
        public void Write()
        {
#if !DEBUG
            if (!controller.IsOpen)
                return;
#endif
            StringBuilder sb = null;
            if(debug)
            {
                sb = new StringBuilder().AppendLine("DMX frames by fixture:");
                debug = false;
            }

            foreach (DmxFixture fixture in fixtures)
            {
                DmxFrame frame = fixture.GetFrame();
                if(sb != null)
                {
                    sb.Append(fixture.FixtureId).Append(" dmx frame: ");
                    foreach(byte b in frame.Data)
                        sb.Append(b).Append(',');
                    if (frame.Data.Length > 0)
                        sb.Length--;
                    sb.AppendLine();
                }

                controller.SetChannels(frame.StartAddress, frame.Data);
            }

            if (sb != null)
                LogFile.Info(sb.ToString());

#if DEBUG
            if(controller.IsOpen)
#endif
            controller.WriteData();
        }

        internal void WriteDebug()
        {
            debug = true;
        }
    }
}
