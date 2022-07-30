using NAudio.Midi;
using System.Collections.Generic;

namespace LightController.Midi
{
    public class MidiDeviceList
    {
        private Dictionary<string, int> midiDevices = new Dictionary<string, int>();
        
        public MidiDeviceList()
        {
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                midiDevices[MidiIn.DeviceInfo(device).ProductName.Trim()] = device;
        }

        public bool TryGetDevice(string name, out MidiInput device)
        {
            name = name.Trim();
            if(midiDevices.TryGetValue(name, out int index))
            {
                if(TryGetDevice(index, out MidiIn input))
                {
                    device = new MidiInput(input, name);
                    return true;
                }

            }

            device = null;
            return false;
        }

        private bool TryGetDevice(int index, out MidiIn input)
        {
            if(index < 0 || index >= MidiIn.NumberOfDevices)
            {
                input = null;
                return false;
            }

            input = new MidiIn(index);
            return true;
        }
    }
}
