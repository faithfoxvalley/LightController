using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO.BACnet.Serialize;
using System.Text;

namespace LightController.Midi
{
    public class MidiDeviceList
    {
        private Dictionary<string, int> midiDevices = new Dictionary<string, int>();
        
        public MidiDeviceList()
        {
            UpdateMidiDeviceList();
        }

        public void UpdateMidiDeviceList()
        {
            midiDevices.Clear();
            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
                midiDevices[MidiIn.DeviceInfo(device).ProductName.Trim().ToLower()] = device;
        }

        public bool TryGetInput(string deviceNames, out MidiInput input)
        {
            string[] names = deviceNames.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            List<MidiIn> devices = new List<MidiIn>();
            foreach(string name in names)
            {
                if (midiDevices.TryGetValue(name.ToLower(), out int index) && TryGetDevice(index, out MidiIn device))
                    devices.Add(device);
            }

            if (devices.Count > 0)
            {
                input = new MidiInput(devices, deviceNames);
                return true;
            }
            input = null;
            return false;
        }

        public bool TryGetAnyInput(out MidiInput input)
        {
            StringBuilder name = new StringBuilder();
            List<MidiIn> devices = new List<MidiIn>();
            for(int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if(TryGetDevice(i, out MidiIn device))
                {
                    if (name.Length > 0)
                        name.Append(", ");
                    name.Append(MidiIn.DeviceInfo(i).ProductName.Trim());
                    devices.Add(device);
                }
            }

            if (devices.Count > 0)
            {
                input = new MidiInput(devices, name.ToString());
                return true;
            }
            input = null;
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
