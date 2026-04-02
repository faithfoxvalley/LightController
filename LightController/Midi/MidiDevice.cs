using NAudio.Midi;
using System;
using System.Collections.Generic;

namespace LightController.Midi;

public class MidiDevice
{
    public string Name { get; }

    private readonly MidiIn midi;

    private MidiDevice(string name, MidiIn midi)
    {
        Name = name;
        this.midi = midi;
        midi.MessageReceived += OnMessageReceived;
        midi.Start();
    }

    private MidiDevice(int index) : this(GetDeviceName(index), new MidiIn(index))
    {

    }

    public static IEnumerable<string> GetDeviceNames()
    {
        List<string> devices = new List<string>();
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            devices.Add(GetDeviceName(i));
        return devices;
    }

    public static bool TryStart(string name, out MidiDevice device)
    {
        name = name.Trim().ToLower();
        device = null;

        if (MidiIn.NumberOfDevices == 0)
            return false;

        if(string.IsNullOrEmpty(name))
        {
            device = new MidiDevice(0);
            return true;
        }

        if(!TryFind(name, out MidiIn midi))
            return false;

        device = new MidiDevice(name, midi);
        return true;
    }

    private static bool TryFind(string name, out MidiIn device)
    {
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            if (GetDeviceName(i) == name)
            {
                device = new MidiIn(i);
                return true;
            }
        }

        device = null;
        return false;
    }

    private static string GetDeviceName(int index)
    {
        return MidiIn.DeviceInfo(index).ProductName.Trim().ToLower();
    }

    private void OnMessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is NoteEvent note)
            NoteEvent?.Invoke(new MidiNote(note));
    }

    public event Action<MidiNote> NoteEvent;
}
