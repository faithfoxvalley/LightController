/*
MIT License

Copyright (c) 2021 Wojciech Berdowski

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using LightController;
using OpenDMX.NET.FTDI;
using System;
using System.Text;

namespace OpenDMX.NET
{
    public class DmxController : IDisposable
    {
        public bool IsOpen { get => handle != IntPtr.Zero; }
        public volatile bool IsDisposed;

        private byte[] buffer = new byte[513];
        private IntPtr handle = IntPtr.Zero;
        private Status status;

        /// <summary>
        /// Creates a new OpenDMX instance.
        /// </summary>
        public DmxController()
        {
        }

        /// <summary>
        /// Initializes device of a given index.
        /// </summary>
        /// <exception cref="OpenDMXException"></exception>
        public void Open(uint deviceIndex)
        {
            status = FTD2XX.Open(deviceIndex, ref handle);
            status = FTD2XX.ResetDevice(handle);
            status = FTD2XX.SetDivisor(handle, (char)12);
            status = FTD2XX.SetDataCharacteristics(handle, DataBits.Bits8, StopBits.StopBits2, Parity.None);
            status = FTD2XX.SetFlowControl(handle, FlowControl.None, 0, 0);
            status = FTD2XX.ClrRts(handle);

            if (status != Status.Ok)
            {
                throw new OpenDMXException("Device could not be initialized.", status);
            }

            ClearBuffer();

        }

        /// <summary>
        /// Sets value of a single channel.
        /// </summary>
        public void SetChannel(int channel, byte value)
        {
            if (channel < 1 || channel > 512)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), "Channel number must be between 1 and 512.");
            }

            buffer[channel] = value;
        }

        public void WriteDebugInfo(StringBuilder sb, int columns)
        {
            sb.Append("___|_");
            for (int i = 0; i < columns; i++)
            {
                if (i < 100)
                    sb.Append('_');
                if (i < 10)
                    sb.Append('_');
                sb.Append(i).Append('_');
            }

            int column = 0;
            for(int i = 1; i < buffer.Length; i++)
            {
                if(column % columns == 0)
                {
                    sb.AppendLine();
                    if (i < 100)
                        sb.Append(' ');
                    if (i < 10)
                        sb.Append(' ');
                    sb.Append(i).Append("| ");
                }

                byte b = buffer[i];
                if (b < 100)
                    sb.Append(' ');
                if (b < 10)
                    sb.Append(' ');
                sb.Append(b).Append(' ');

                column++;
            }
        }

        /// <summary>
        /// Sets values of a channel range.
        /// </summary>
        public void SetChannels(int startChannel, byte[] values)
        {
            if (startChannel < 1 || startChannel + values.Length > 512)
            {
                throw new ArgumentOutOfRangeException(nameof(startChannel), "Start channel number must be between 1 and 512.");
            }

            Buffer.BlockCopy(values, 0, buffer, startChannel, values.Length);
        }

        public Device[] GetDevices()
        {
            uint count = 0;

            status = FTD2XX.CreateDeviceInfoList(ref count);
            if (status != Status.Ok)
            {
                throw new OpenDMXException("Could not get devices count.", status);
            }

            Device[] devices = new Device[count];
            byte[] serial = new byte[16];
            byte[] description = new byte[64];

            for (uint i = 0; i < count; i++)
            {
                devices[i] = new Device();

                status = FTD2XX.GetDeviceInfoDetail(i, ref devices[i].Flags, ref devices[i].Type, ref devices[i].ID, ref devices[i].LocId, serial, description, ref devices[i].ftHandle);
                if (status != Status.Ok)
                {
                    throw new OpenDMXException("Could not get device info.", status);
                }

                devices[i].DeviceIndex = i;
                devices[i].SerialNumber = Encoding.ASCII.GetString(serial);
                devices[i].Description = Encoding.ASCII.GetString(description);

                int nullIndex = devices[i].SerialNumber.IndexOf('\0');
                if (nullIndex != -1)
                    devices[i].SerialNumber = devices[i].SerialNumber.Substring(0, nullIndex);

                nullIndex = devices[i].Description.IndexOf('\0');
                if (nullIndex != -1)
                    devices[i].Description = devices[i].Description.Substring(0, nullIndex);
            }

            if (status != Status.Ok)
            {
                throw new OpenDMXException("Could not get devices list.", status);
            }

            return devices;
        }

        public void WriteData()
        {
            if (!IsDisposed)
                WriteBuffer();
        }

        private void WriteBuffer()
        {
            status = FTD2XX.Purge(handle, Purge.PurgeTx);
            status = FTD2XX.Purge(handle, Purge.PurgeRx);
            status = FTD2XX.SetBreakOn(handle);
            status = FTD2XX.SetBreakOff(handle);

            uint bytesWritten = 0;
            status = FTD2XX.Write(handle, buffer, (uint)buffer.Length, ref bytesWritten);

            if (bytesWritten != buffer.Length)
                LogFile.Warn($"Unable to write {buffer.Length} bytes to DMX device, only wrote {bytesWritten} bytes.");

            if (status != Status.Ok)
                throw new OpenDMXException("Data write error.", status);
        }

        private void ClearBuffer()
        {
            Array.Clear(buffer, 0, buffer.Length);
            WriteBuffer();
        }

        public void Dispose()
        {
            IsDisposed = true;

            if (IsOpen)
            {
                ClearBuffer();
                FTD2XX.ResetDevice(handle);
                FTD2XX.Close(handle);
            }
        }
    }

}
