using ENTTEC.Devices.Data;
using FTD2XX_NET;
using System;
using System.IO;
using System.Text;
using System.Threading;

namespace LightController.Dmx;

internal class DmxUsbProController : IDmxController
{
    private readonly FTDI ftdi;
    private readonly byte[] buffer = new byte[513];
    private int bufferOffset = 0;
    private object writeLock = new object();
    private bool ready = true;

    public bool IsOpen => ftdi.IsOpen && ready;

    internal DmxUsbProController(FTDI ftdi)
    {
        this.ftdi = ftdi;

        Array.Clear(buffer, 0, buffer.Length);
        buffer = DmxUsbProUtils.CreatePacketForDevice(DmxUsbProConstants.SEND_DMX_PACKET_REQUEST_LABEL, buffer);
        bufferOffset = DmxUsbProConstants.PacketDataStartPosition;

        if (!ftdi.IsOpen)
            return;
        Purge();
        WriteData();
    }

    public void SetChannel(int channel, byte value)
    {
        if (channel < 1 || channel > 512)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), "Channel number must be between 1 and 512.");
        }

        buffer[channel + bufferOffset] = value;
    }

    public void SetChannels(int startChannel, byte[] values)
    {
        int endChannel = startChannel + values.Length;

        if (startChannel < 1 || endChannel > 512)
        {
            throw new ArgumentOutOfRangeException(nameof(startChannel), "Start channel number must be between 1 and 512.");
        }

        Buffer.BlockCopy(values, 0, buffer, startChannel + bufferOffset, values.Length);
    }

    public void WriteData()
    {
        lock (writeLock)
        {
            if (!IsOpen)
                return;
            
            //Purge();

            uint bytesWritten = 0;
            FTDI.FT_STATUS status = ftdi.Write(buffer, buffer.Length, ref bytesWritten);
            if (status != FTDI.FT_STATUS.FT_OK || bytesWritten == 0)
                throw new DmxException("Error occurred while writing dmx data: " + status, status);

            if (bytesWritten != buffer.Length)
                Log.Warn($"Unable to write {buffer.Length} bytes to DMX device, only wrote {bytesWritten} bytes.");
        }
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
        for (int i = 1 + bufferOffset; i < (513 + bufferOffset); i++)
        {
            int dmxChannel = i - bufferOffset;
            if (column % columns == 0)
            {
                sb.AppendLine();
                if (dmxChannel < 100)
                    sb.Append(' ');
                if (dmxChannel < 10)
                    sb.Append(' ');
                sb.Append(dmxChannel).Append("| ");
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

    public void Dispose()
    {
        if (!ftdi.IsOpen)
            return;

        CloseDmxPro();

        if (!CheckStatusCode(ftdi.Close(), "Failed finalize close of DMX device"))
            return;

    }

    private void CloseDmxPro()
    {
        /* ENTTEC API:
         * "The periodic DMX packet output will stop and the Widget DMX port direction will change to input when the
         * Widget receives any request message other than the Output Only Send DMX Packet request, or the Get
         * Widget Parameters request." 
         */

        lock (writeLock)
        {
            ready = false;

            byte[] buffer = DmxUsbProUtils.CreatePacketForDevice(DmxUsbProConstants.GET_WIDGET_SERIAL_NUMBER_LABEL);

            if (!CheckStatusCode(ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_TX), "Failed to purge DMX device during close (1)"))
                return;
            if (!CheckStatusCode(ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX), "Failed to purge DMX device during close (2)"))
                return;

            if (!CheckStatusCode(ftdi.SetBreak(true), "Failed to set break for DMX device during close"))
                return;
            if (!CheckStatusCode(ftdi.SetBreak(false), "Failed to reset break for DMX device during close"))
                return;

            uint bytesWritten = 0;
            if (!CheckStatusCode(ftdi.Write(buffer, buffer.Length, ref bytesWritten), bytesWritten, "Failed to write closing data to DMX device"))
                return;
        }
    }

    private static bool CheckStatusCode(FTDI.FT_STATUS status, string error)
    {
        if (status != FTDI.FT_STATUS.FT_OK)
        {
            Log.Error($"{error} (error code {status})");
            return false;
        }
        return true;
    }

    private static bool CheckStatusCode(FTDI.FT_STATUS status, uint bytesWritten, string error)
    {
        if (status != FTDI.FT_STATUS.FT_OK || bytesWritten == 0)
        {
            Log.Error($"{error} (error code {status})");
            return false;
        }
        return true;
    }

    public bool TryReadDmxFrame(out byte[] data)
    {
        data = null;

        lock (writeLock)
        {
            if (!IsOpen)
                return false;

            //Purge();

            byte[] setReceivePacket = DmxUsbProUtils.CreatePacketForDevice(DmxUsbProConstants.SET_RECEIVE_DMX_ON_CHANGE_LABEL, [0]);

            uint bytesWritten = 0;
            if (!CheckStatusCode(ftdi.Write(setReceivePacket, setReceivePacket.Length, ref bytesWritten), bytesWritten, "Failed to write DMX receive mode to device"))
                return false;

            //Purge();

            bytesWritten = 0;
            byte[] buffer = new byte[1024];

            if (!CheckStatusCode(ftdi.Read(buffer, (uint)buffer.Length, ref bytesWritten), bytesWritten, "Failed to read dmx data"))
                return false;

            if (!TryGetDmxData(buffer, out data))
                return false;

            return true;
        }

    }

    private void Purge()
    {
        ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_TX);
        ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX);
        ftdi.SetBreak(true);
        ftdi.SetBreak(false);
    }

    private bool TryGetDmxData(byte[] buffer, out byte[] data)
    {
        data = null;
        try
        {
            using BinaryReader reader = new BinaryReader(new MemoryStream(buffer));

            while (reader.ReadByte() != DmxUsbProConstants.DMX_START_CODE)
                ;

            if (reader.ReadByte() != DmxUsbProConstants.RECEIVED_DMX_PACKET_LABEL)
                return false;

            ushort dataLength = reader.ReadUInt16(); // must be little endian
            if(dataLength < 2)
                return false;
            // Packet data start
            if (reader.ReadByte() != 0)
                return false; // Device reports error occurred

            data = reader.ReadBytes(dataLength - 1);
            // Packet data end
            if (reader.ReadByte() != DmxUsbProConstants.DMX_END_CODE)
                return false;

            return true;
        }
        catch (IOException e)
        {
            return false;
        }

    }
}
