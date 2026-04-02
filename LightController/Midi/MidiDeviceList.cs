using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightController.Midi;

public class MidiDeviceList
{
    private readonly Dictionary<string, MidiDevice> activeDevices = new Dictionary<string, MidiDevice>();

    public MidiDeviceList()
    {
    }

    public bool TryGetInput(string deviceNames, out MidiInput input)
    {
        string[] names = deviceNames.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return TryGetInput(names, out input);
    }

    public bool TryGetAnyInput(out MidiInput input)
    {
        return TryGetInput(MidiDevice.GetDeviceNames(), out input);
    }

    public bool TryGetInput(IEnumerable<string> deviceNames, out MidiInput input)
    {
        List<MidiDevice> devices = new List<MidiDevice>();
        foreach (string name in deviceNames)
        {
            if (TryGetDevice(name, out MidiDevice device))
                devices.Add(device);
        }

        if (devices.Count <= 0)
        {
            input = null;
            return false;
        }

        input = new MidiInput(devices);
        return true;
    }

    private bool TryGetDevice(string name, out MidiDevice device)
    {
        name = name.Trim().ToLower();

        if (activeDevices.TryGetValue(name, out device))
            return true;

        if (MidiDevice.TryStart(name, out device))
        {
            activeDevices[name] = device;
            return true;
        }

        Log.Warn($"Midi device '{name}' not found");
        return false;
    }

    internal void LogMidiDeviceList()
    {
        StringBuilder sb = new StringBuilder();
        string[] deviceNames = MidiDevice.GetDeviceNames().ToArray();
        sb.Append("Midi Devices: (").Append(deviceNames.Length).Append(')').AppendLine();

        foreach(string name in deviceNames)
        {
            sb.Append(name);
            if (activeDevices.ContainsKey(name))
                sb.Append(" (active)");
            sb.AppendLine();
        }

        Log.Info(sb.ToString());
    }
}
