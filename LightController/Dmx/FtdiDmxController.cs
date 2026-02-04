using FTD2XX_NET;
using System;
using System.Text;

namespace LightController.Dmx;

public class FtdiDmxController : IDmxController, IDisposable
{
    private readonly FTDI ftdi;
    private readonly byte[] buffer = new byte[513];
    private int bufferOffset = 0;
    private object writeLock = new object();

    public bool IsOpen => ftdi.IsOpen;

    public string Name { get; private set; }

    private FtdiDmxController(FTDI ftdi, string name)
    {
        this.ftdi = ftdi;

        Array.Clear(buffer, 0, buffer.Length);

        WriteData();
        Name = name;
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
            if(!IsOpen)
                return;

            ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_TX);
            ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX);
            ftdi.SetBreak(true);
            ftdi.SetBreak(false);

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

    public static void LogCurrentDevices()
    {
        FTDI ftdi = new FTDI();
        uint deviceCount = 0;

        // Look for devices
        if (!CheckStatusCode(ftdi.GetNumberOfDevices(ref deviceCount), "Failed to get number of dmx devices"))
            return;

        Log.Info("Number of FTDI devices: " + deviceCount.ToString());

        if (deviceCount == 0)
            return;

        FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];

        // Populate our device list
        if (!CheckStatusCode(ftdi.GetDeviceList(ftdiDeviceList), "Failed to get dmx device list"))
            return;
        foreach (var deviceInfo in ftdiDeviceList)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DMX device:");
            sb.AppendLine("Flags: " + string.Format("{0:x}", deviceInfo.Flags));
            sb.AppendLine("Type: " + deviceInfo.Type.ToString());
            sb.AppendLine("ID: " + string.Format("{0:x}", deviceInfo.ID));
            sb.AppendLine("Location ID: " + string.Format("{0:x}", deviceInfo.LocId));
            sb.AppendLine("Serial Number: " + deviceInfo.SerialNumber.ToString());
            sb.AppendLine("Description: " + deviceInfo.Description.ToString());
            Log.Info(sb.ToString());
        }
    }

    public static bool TryOpenDevice(string serialNumber, out IDmxController controller)
    {
        controller = null;

        try
        {
            FTDI ftdi = new FTDI();
            uint deviceCount = 0;

            // Look for devices
            if (!CheckStatusCode(ftdi.GetNumberOfDevices(ref deviceCount), "Failed to get number of dmx devices"))
                return false;

            if (deviceCount == 0)
                return false;

            FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];

            // Populate our device list
            if (!CheckStatusCode(ftdi.GetDeviceList(ftdiDeviceList), "Failed to get dmx device list"))
                return false;

            FTDI.FT_DEVICE_INFO_NODE deviceInfo = null;
            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                deviceInfo = ftdiDeviceList[0];
            }
            else
            {
                foreach(var ftdiDevice in ftdiDeviceList)
                {
                    if (ftdiDevice.SerialNumber == serialNumber)
                    {
                        deviceInfo = ftdiDevice;
                        break;
                    }
                }
                if(deviceInfo == null)
                    return false;
            }

            Log.Info("Opening DMX device:" + deviceInfo.SerialNumber.ToString());

            if (!CheckStatusCode(ftdi.OpenBySerialNumber(deviceInfo.SerialNumber), "Failed to open device"))
                return false;

            if (!CheckStatusCode(ftdi.ResetDevice(), "Failed to reset device"))
                return false;

            if (!CheckStatusCode(ftdi.SetBaudRate(250000), "Failed to set device baud rate"))
                return false;

            if (!CheckStatusCode(ftdi.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_2, FTDI.FT_PARITY.FT_PARITY_NONE), "Failed to set device data characteristics"))
                return false;

            if (!CheckStatusCode(ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0, 0), "Failed to set device flow control"))
                return false;

            if (!CheckStatusCode(ftdi.SetRTS(false), "Failed to set device rts"))
                return false;

            if (!CheckStatusCode(ftdi.SetTimeouts(5000, 1000), "Failed to set device timeouts"))
                return false;

            if (deviceInfo.Description == "DMX USB PRO")
                controller = new DmxUsbProController(ftdi, deviceInfo.SerialNumber);
            else
                controller = new FtdiDmxController(ftdi, deviceInfo.SerialNumber);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error occurred while accessing dmx device: ");
            return false;
        }
    }

    public void Dispose()
    {
        if (!ftdi.IsOpen)
            return;

        if (!CheckStatusCode(ftdi.Close(), "Failed finalize close of DMX device"))
            return;

    }

    private static bool CheckStatusCode(FTDI.FT_STATUS status, string error)
    {
        if(status != FTDI.FT_STATUS.FT_OK)
        {
            Log.Error($"{error} (error code {status})");
            return false;
        }
        return true;
    }

    public bool TryReadDmxFrame(out byte[] data)
    {
        data = null;
        return false;
    }
}
