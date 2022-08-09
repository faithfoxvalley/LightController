using NAudio.Midi;
using System;

namespace LightController.Midi
{
    public class MidiInput
    {
        public string Name { get; }
        public MidiIn Input { get; }

        public MidiInput(MidiIn input, string name)
        {
            Input = input;
            Name = name;
            input.MessageReceived += Input_MessageReceived;
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


    }
}
