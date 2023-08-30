using LightController.ArtNet;
using LightController.Config;
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
        private ArtNetController artnet = new ArtNetController();

        public DmxProcessor(DmxConfig config)
        {
            if(config == null)
            {
                ErrorBox.Show("No DMX settings found, please check your config.");
                return;
            }

            // TODO: Refactor so that dmx OR artnet can be used
            if(config.ArtNet)
            {
                while (!artnet.TryOpenSocket(config.ArtNetAddress))
                {
#if DEBUG
                    break;
#else
                    ErrorBox.ExitOnCancel("Art-Net compatible interface not found. Press OK to try again or Cancel to exit."); 
#endif
                }
            }
            else
            {
                while (!OpenDevice(config.DmxDevice))
                {
#if DEBUG
                    break;
#else
                    ErrorBox.ExitOnCancel("DMX Device not found. Press OK to try again or Cancel to exit."); 
#endif
                }
            }

            if (config.Addresses == null || config.Addresses.Count == 0)
            {
                LogFile.Warn("No DMX fixture addresses found.");
                return;
            }

            Dictionary<string, DmxDeviceProfile> profiles;
            if (config.Fixtures != null)
                profiles = config.Fixtures.ToDictionary(x => x.Name);
            else
                profiles = new Dictionary<string, DmxDeviceProfile>();

            List<DmxDeviceAddress> addresses = config.Addresses;
            foreach(DmxDeviceAddress fixtureAddress in addresses)
            {
                if(fixtureAddress.Name == null)
                {
                    LogFile.Error("DMX fixture with address " + fixtureAddress.StartAddress + " does not contain a fixture profile name.");
                }
                else if(fixtureAddress.Count < 1)
                {
                    LogFile.Error("DMX address for fixture '" + fixtureAddress.Name + "' must have a count that is at least 1.");
                }
                else if(fixtureAddress.StartAddress < 1)
                {
                    LogFile.Error("DMX address for fixture '" + fixtureAddress.Name + "' must have a start address that is at least 1.");
                }
                else if(profiles.TryGetValue(fixtureAddress.Name, out DmxDeviceProfile profile))
                {
                    if(profile.DmxLength < 1)
                    {
                        LogFile.Error("DMX profile for fixture '" + profile.Name + "' must have a dmx length of at least one.");
                    }
                    else if(profile.DmxLength < profile.AddressMap.Count)
                    {
                        LogFile.Error("DMX profile for fixture '" + profile.Name + "' has more defined channels than its dmx length.");
                    }
                    else
                    {
                        int address = fixtureAddress.StartAddress;
                        for (int i = 0; i < fixtureAddress.Count; i++)
                        {
                            fixtures.Add(new DmxFixture(profile, address, fixtures.Count + 1));
                            address += profile.DmxLength;
                        }
                    }
                }
                else
                {
                    LogFile.Error("No DMX fixture profile with name '" + fixtureAddress.Name + "' found.");
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
            try
            {
                var devices = controller.GetDevices();
                if (deviceIndex < devices.Length)
                {
                    controller.Open(deviceIndex);
                    return true;
                }
            } catch { }
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

        public void SetInputs(IEnumerable<Config.Input.InputBase> inputs, Animation animation)
        {
            foreach (DmxFixture fixture in fixtures)
                fixture.SetInput(inputs, animation.GetLength(fixture.FixtureId), animation.GetDelay(fixture.FixtureId));
        }
        
        public void Write()
        {
#if !DEBUG
            if (!controller.IsOpen && !artnet.IsOpen)
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

                if(controller.IsOpen)
                    controller.SetChannels(frame.StartAddress, frame.Data);
                if (artnet.IsOpen)
                    artnet.SetChannels(frame.StartAddress, frame.Data);
            }

            if (sb != null)
                LogFile.Info(sb.ToString());

#if DEBUG
            if(controller.IsOpen)
#endif
            controller.WriteData();

#if DEBUG
            if (artnet.IsOpen)
#endif
            artnet.WriteData();
        }

        public void WriteDebug()
        {
            debug = true;
        }
    }
}
