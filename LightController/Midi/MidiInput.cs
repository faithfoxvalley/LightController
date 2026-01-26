using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightController.Midi;

public class MidiInput
{
    public string Name { get; }

    private readonly MidiIn[] input;

    public MidiInput(IEnumerable<MidiIn> input, string name)
    {
        this.input = input.ToArray();
        Name = name ?? "unknown";
        foreach(MidiIn i in this.input)
            i.MessageReceived += Input_MessageReceived;
    }

    private void Input_MessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if(e.MidiEvent is NoteEvent note)
        {
            if (NoteEvent != null)
                NoteEvent.Invoke(new MidiNote(note));
        }
    }

    public event Action<MidiNote> NoteEvent;

    public void Start()
    {
        foreach (MidiIn i in input)
            i.Start();

    }
}
