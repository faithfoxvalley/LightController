using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Windows.Devices.Enumeration;

namespace LightController.Midi;

public class MidiDevice
{
    public string Name { get; }

    private readonly IMidiInput midi;

    private MidiDevice(string name, WinRTMidiIn midi)
    {
        Name = name;
        this.midi = midi;
        midi.MessageReceived += OnMessageReceived;
        midi.Start();
    }

    public static IEnumerable<string> GetDeviceNames()
    {
        return AsyncUtils.Run(WinRTMidiIn.GetDevicesAsync).Select(x => x.Name).ToArray();
    }

    private static bool TryGetDeviceId(string name, out string id)
    {
        id = null;

        var devices = AsyncUtils.Run(WinRTMidiIn.GetDevicesAsync);
        foreach(DeviceInformation info in devices)
        {
            if (info.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
            {
                id = info.Id;
                return true;
            }
        }

        return false;
    }

    public static bool TryStart(string name, out MidiDevice device)
    {
        device = null;

        if (!TryGetDeviceId(name.Trim(), out string id))
            return false;

        return TryStart(name, id, out device);
    }

    public static IEnumerable<MidiDevice> StartAll()
    {
        List<MidiDevice> result = new List<MidiDevice>();

        var devices = AsyncUtils.Run(WinRTMidiIn.GetDevicesAsync);
        foreach (DeviceInformation info in devices)
        {
            if(TryStart(info.Name, info.Id, out MidiDevice device))
                result.Add(device);
        }

        return result;
    }

    private static bool TryStart(string name, string id, out MidiDevice device)
    {
        device = null;

        try
        {
            WinRTMidiIn midi = AsyncUtils.Run(WinRTMidiIn.CreateAsync, id);
            device = new MidiDevice(name, midi);
            return true;
        }
        catch (InvalidOperationException ex)
        { }
        return false;
    }


    private void OnMessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is NoteEvent note)
            NoteEvent?.Invoke(new MidiNote(note));
    }

    public event Action<MidiNote> NoteEvent;
}
