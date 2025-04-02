using LightController.Config.Animation;
using LightController.Config.Dmx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace LightController.Dmx
{
    public class DmxProcessor
    {
        private bool debug;
        private List<DmxFixture> fixtures = new List<DmxFixture>();
        private IDmxController controller = new NullDmxController();
        public PreviewWindow Preview { get; set; }

        public DmxProcessor(DmxConfig config)
        {
            if(config == null)
            {
                ErrorBox.Show("No DMX settings found, please check your config.");
                return;
            }

            while (!OpenDevice((int)config.DmxDevice))
            {
#if DEBUG
                break;
#else
                ErrorBox.ExitOnCancel("DMX Device not found. Press OK to try again or Cancel to exit."); 
#endif
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

        private bool OpenDevice(int device)
        {
            if(FtdiDmxController.TryOpenDevice(device, out FtdiDmxController controller))
            {
                this.controller = controller;
                return true;
            }
            this.controller = new NullDmxController();
            return false;
        }

        /// <summary>
        /// Turns off all fixtures
        /// </summary>
        public void TurnOff()
        {
            LogFile.Info("Turning off DMX device and fixtures");

            foreach (DmxFixture fixture in fixtures)
                fixture.TurnOff();
            Write();
            Thread.Sleep(500); // Allow the DMX device to transmit the empty frame before shutting down
            if (controller.IsOpen)
                controller.Dispose();

            LogFile.Info("Turned off DMX");
        }

        public void SetInputs(IEnumerable<Config.Input.InputBase> inputs, TransitionAnimation transition)
        {
            foreach (DmxFixture fixture in fixtures)
            {
                transition.GetMixDetails(fixture.FixtureId, out double mixLength, out double mixDelay);
                fixture.SetInput(inputs, mixLength, mixDelay);
            }
        }

        
        public void Write()
        {
#if !DEBUG
            if (!controller.IsOpen)
                return;
#endif
            Dictionary<int, DmxFrame> frames = null;
            if (Preview != null)
                frames = new Dictionary<int, DmxFrame>();

            foreach (DmxFixture fixture in fixtures)
            {
                DmxFrame frame = fixture.GetFrame();
                controller.SetChannels(frame.StartAddress, frame.Data);
                if (frames != null)
                    frames[fixture.FixtureId] = frame;
            }

            PreviewWindow preview = Preview;
            if (preview != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (DmxFixture fixture in fixtures)
                    {
                        DmxFrame frame = frames[fixture.FixtureId];
                        preview.SetPreviewColor(fixture.FixtureId, frame.PreviewColor, frame.PreviewIntensity);
                    }
                });
            }

            if (debug)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("DMX data:");
                controller.WriteDebugInfo(sb, 8);
                debug = false;
                LogFile.Info(sb.ToString());
            }

            if(controller.IsOpen)
                controller.WriteData();
        }

        public void WriteDebug()
        {
            debug = true;
        }

        public void InitPreview(PreviewWindow preview)
        {
            preview.Init(fixtures);

        }
    }
}
