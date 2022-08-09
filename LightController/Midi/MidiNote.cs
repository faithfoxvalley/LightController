using NAudio.Midi;
using System;
using System.Collections.Generic;

namespace LightController.Midi
{
    /// <summary>
    /// Serializable class to store a midi note
    /// </summary>
    public class MidiNote : IEquatable<MidiNote>
    {
        public int Channel { get; set; }
        public int Note { get; set; }

        public MidiNote()
        {

        }

        public MidiNote(NoteEvent note)
        {
            Channel = note.Channel;
            Note = note.NoteNumber;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MidiNote);
        }

        public bool Equals(MidiNote other)
        {
            return other is not null &&
                   Channel == other.Channel &&
                   Note == other.Note;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Channel, Note);
        }

        public static bool operator ==(MidiNote left, MidiNote right)
        {
            return EqualityComparer<MidiNote>.Default.Equals(left, right);
        }

        public static bool operator !=(MidiNote left, MidiNote right)
        {
            return !(left == right);
        }
    }
}
