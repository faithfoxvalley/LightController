using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LightController.Midi;

public class MidiInput
{
    public string Name { get; }

    private readonly MidiDevice[] input;

    public MidiInput(IEnumerable<MidiDevice> input)
    {
        this.input = input.ToArray();
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < this.input.Length; i++)
        {
            MidiDevice dev = this.input[i];
            dev.NoteEvent += OnNoteEvent;
            if (i > 0)
                sb.Append(", ");
            sb.Append(dev.Name);
        }
        Name = sb.ToString();
    }

    private void OnNoteEvent(MidiNote note)
    {
        NoteEvent?.Invoke(note);
    }

    public event Action<MidiNote> NoteEvent;
}
