using LightController.Config.Animation;
using LightController.Config.Dmx;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace LightController.Dmx;

public class DmxProcessor
{
    private List<DmxFixture> fixtures = new List<DmxFixture>();
    private List<DmxUniverse> universes = new List<DmxUniverse>();

    public DmxProcessor(DmxConfig config, int dmxFps)
    {
        if(config == null)
        {
            ErrorBox.Show("No DMX settings found, please check your config.");
            return;
        }

        FtdiDmxController.LogCurrentDevices();

        if (config.Addresses == null || config.Addresses.Count == 0)
        {
            Log.Warn("No DMX fixture addresses found.");
            return;
        }

        if (config.Interfaces == null || config.Interfaces.Count == 0)
        {
            universes.Add(new DmxUniverse(null, dmxFps, config.DeviceOptional));
        }
        else
        {
            List<int> universeIds = config.Addresses.Select(x => x.Universe).ToList();
            for (int i = 0; i < config.Interfaces.Count; i++)
            {
                string dmxInterface = config.Interfaces[i];
                if (string.IsNullOrWhiteSpace(dmxInterface))
                    continue;
                if (universeIds.Contains(i + 1))
                {
                    universes.Add(new DmxUniverse(dmxInterface, dmxFps, config.DeviceOptional));
                }
                else
                {
                    universes.Add(new DmxUniverse());
                    Log.Warn($"DMX device {dmxInterface} was specified as an interface but is not being used by any fixtures");
                }
            }
            if(universes.Count == 0)
            {
                ErrorBox.Show("No DMX devices configured");
                return;
            }    
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
                ErrorBox.Show("DMX fixture with address " + fixtureAddress.StartAddress + " does not contain a fixture profile name.");
                return;
            }

            if(fixtureAddress.Count < 1)
            {
                ErrorBox.Show("DMX address for fixture '" + fixtureAddress.Name + "' must have a count that is at least 1.");
                return;
            }

            if(fixtureAddress.StartAddress < 1)
            {
                ErrorBox.Show("DMX address for fixture '" + fixtureAddress.Name + "' must have a start address that is at least 1.");
                return;
            }

            if(profiles.TryGetValue(fixtureAddress.Name, out DmxDeviceProfile profile))
            {
                if(profile.DmxLength < 1)
                {
                    ErrorBox.Show("DMX profile for fixture '" + profile.Name + "' must have a dmx length of at least one.");
                    return;
                }

                if(profile.DmxLength < profile.AddressMap.Count)
                {
                    ErrorBox.Show("DMX profile for fixture '" + profile.Name + "' has more defined channels than its dmx length.");
                    return;
                }

                int address = fixtureAddress.StartAddress;
                for (int i = 0; i < fixtureAddress.Count; i++)
                {
                    DmxFixture fixture = new DmxFixture(profile, address, fixtures.Count + 1, fixtureAddress.Universe);
                    if(!TryAddFixture(fixture, fixtureAddress.Universe))
                    {
                        ErrorBox.Show($"DMX profile for fixture '{profile.Name}' is configured with a DMX universe that does not exist");
                        return;
                    }
                    fixtures.Add(fixture);
                    address += profile.DmxLength;
                }
            }
            else
            {
                ErrorBox.Show("No DMX fixture profile with name '" + fixtureAddress.Name + "' found.");
                return;
            }
        }
    }

    public void AppendToListbox(System.Windows.Controls.ListBox list)
    {
        list.Items.Clear();
        foreach (DmxFixture fixture in fixtures)
            list.Items.Add(fixture);
    }

    private bool TryAddFixture(DmxFixture fixture, int universe)
    {
        if (universe <= 0 || universe > universes.Count)
            return false;

        universes[universe - 1].AddFixture(fixture);
        return true;
    }

    /// <summary>
    /// Turns off all fixtures
    /// </summary>
    public void TurnOff()
    {
        Log.Info("Turning off DMX device and fixtures");

        foreach (DmxFixture fixture in fixtures)
            fixture.TurnOff();

        foreach (DmxUniverse universe in universes)
            universe.Write();

        Thread.Sleep(500); // Allow the DMX device to transmit the empty frame before shutting down

        foreach (DmxUniverse universe in universes)
            universe.TurnOff();

        Log.Info("Turned off DMX");
    }

    public void SetInputs(IEnumerable<Config.Input.InputBase> inputs, TransitionAnimation transition)
    {
        foreach (DmxFixture fixture in fixtures)
        {
            transition.GetMixDetails(fixture.FixtureId, out double mixLength, out double mixDelay);
            fixture.SetInput(inputs, mixLength, mixDelay);
        }
    }

    internal void AppendPerformanceInfo(StringBuilder sb)
    {
        foreach (DmxUniverse universe in universes)
            universe.AppendPerformanceInfo(sb);
    }

    internal void WriteDebug()
    {
        foreach (DmxUniverse universe in universes)
            universe.WriteDebug();
    }

    internal void InitPreview(PreviewWindow preview)
    {
        preview.Init(fixtures);
        foreach (DmxUniverse universe in universes)
            universe.InitPreview(preview);
    }

    internal void ClosePreview()
    {
        foreach (DmxUniverse universe in universes)
            universe.ClosePreview();
    }
}
