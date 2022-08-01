using NAudio.Midi;
using System;
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

        public bool TryGetFirstDevice(out MidiInput device)
        {
            if (MidiIn.NumberOfDevices > 0 && TryGetDevice(0, out MidiIn result))
            {
                device = new MidiInput(result, MidiIn.DeviceInfo(0).ProductName.Trim());
                return true;
            }
            device = null;
            return false;
        }

        public MidiInput FirstOrDefault()
        {
            if(MidiIn.NumberOfDevices > 0 && TryGetDevice(0, out MidiIn result))
                return new MidiInput(result, MidiIn.DeviceInfo(0).ProductName.Trim());
            return null;
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
