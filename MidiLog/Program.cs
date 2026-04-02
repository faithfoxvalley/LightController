using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MidiLog;

internal class Program
{
    private static Stopwatch sw;

    private static async Task Main(string[] args)
    {
        Dictionary<string, int> midiDevices = new();
        for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            midiDevices[MidiIn.DeviceInfo(device).ProductName.Trim().ToLower()] = device;

        MidiIn midi = new MidiIn(midiDevices["stagemidi"]);
        midi.MessageReceived += Midi_MessageReceived;
        midi.Start();

        sw = new Stopwatch();
        sw.Start();

        Console.WriteLine("Started");
        await Task.Delay(100000000);
    }

    private static void Midi_MessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent is NoteEvent note && note.Velocity > 0)
        {
            string name = note.NoteName.ToLowerInvariant();
            ConsoleColor color = ConsoleColor.White;
            if (note.CommandCode != MidiCommandCode.NoteOn)
                color = ConsoleColor.White;
            else if (name.Contains("bass"))
                color = ConsoleColor.Cyan;
            else if (name.Contains("drum") || name.Contains("snare") || name.Contains("tom"))
                color = ConsoleColor.Blue;
            else if (name.Contains("cymbal") || name.Contains("hi-hat") || name.Contains("crash"))
                color = ConsoleColor.Yellow;
            Console.ForegroundColor = color;
            Console.WriteLine($"{sw.ElapsedMilliseconds} [{note.CommandCode}] CH: {note.Channel} N: {note.NoteNumber} ({note.NoteName}) I: {note.Velocity}");

        }
    }
}