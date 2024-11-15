using FTD2XX_NET;
using System;
using System.Text;

namespace LightController.Dmx
{
    public class FtdiDmxController : IDmxController, IDisposable
    {
        private readonly FTDI ftdi;
        private readonly byte[] buffer = new byte[513];
        private int bufferOffset = 0;
        private bool usbPro;

        public bool IsOpen => ftdi.IsOpen;

        private FtdiDmxController(FTDI ftdi, bool usbPro = false)
        {
            this.ftdi = ftdi;
            this.usbPro = usbPro;

            Array.Clear(buffer, 0, buffer.Length);

            if(usbPro)
            {
                buffer = ENTTEC.Devices.Data.DmxUsbProUtils.CreatePacketForDevice(ENTTEC.Devices.Data.DmxUsbProConstants.SEND_DMX_PACKET_REQUEST_LABEL, buffer);
                bufferOffset = 4;
            }

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
            ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_TX);
            ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX);
            ftdi.SetBreak(true);
            ftdi.SetBreak(false);

            uint bytesWritten = 0;
            FTDI.FT_STATUS status = ftdi.Write(buffer, buffer.Length, ref bytesWritten);
            if(status != FTDI.FT_STATUS.FT_OK)
                throw new DmxException("Error occurred while writing dmx data: " + status, status);

            if (bytesWritten != buffer.Length)
                LogFile.Warn($"Unable to write {buffer.Length} bytes to DMX device, only wrote {bytesWritten} bytes.");
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
                if (column % columns == 0)
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

        public static bool TryOpenDevice(int device, out FtdiDmxController controller)
        {
            controller = null;

            try
            {
                FTDI ftdi = new FTDI();
                uint deviceCount = 0;

                // Look for devices
                FTDI.FT_STATUS status = ftdi.GetNumberOfDevices(ref deviceCount);

                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to get number of dmx devices (error " + status.ToString() + ")");
                    return false;
                }

                LogFile.Info("Number of FTDI devices: " + deviceCount.ToString());

                if (deviceCount == 0)
                    return false;

                FTDI.FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FTDI.FT_DEVICE_INFO_NODE[deviceCount];

                // Populate our device list
                status = ftdi.GetDeviceList(ftdiDeviceList);

                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to get dmx device list (error " + status.ToString() + ")");
                    return false;
                }


                if (device < 0 || device >= ftdiDeviceList.Length)
                    device = 0;

                FTDI.FT_DEVICE_INFO_NODE deviceInfo = ftdiDeviceList[device];
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Opening device with index " + device);
                sb.AppendLine("Flags: " + string.Format("{0:x}", deviceInfo.Flags));
                sb.AppendLine("Type: " + deviceInfo.Type.ToString());
                sb.AppendLine("ID: " + string.Format("{0:x}", deviceInfo.ID));
                sb.AppendLine("Location ID: " + string.Format("{0:x}", deviceInfo.LocId));
                sb.AppendLine("Serial Number: " + deviceInfo.SerialNumber.ToString());
                sb.AppendLine("Description: " + deviceInfo.Description.ToString());
                LogFile.Info(sb.ToString());

                status = ftdi.OpenBySerialNumber(deviceInfo.SerialNumber);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to open device (error " + status.ToString() + ")");
                    return false;
                }

                status = ftdi.ResetDevice();
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to reset device (error " + status.ToString() + ")");
                    return false;
                }

                status = ftdi.SetBaudRate(250000);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to set device baud rate (error " + status.ToString() + ")");
                    return false;
                }

                status = ftdi.SetDataCharacteristics(FTDI.FT_DATA_BITS.FT_BITS_8, FTDI.FT_STOP_BITS.FT_STOP_BITS_2, FTDI.FT_PARITY.FT_PARITY_NONE);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to set device data characteristics (error " + status.ToString() + ")");
                    return false;
                }

                status = ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0, 0);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to set device flow control (error " + status.ToString() + ")");
                    return false;
                }

                status = ftdi.SetRTS(false);
                if (status != FTDI.FT_STATUS.FT_OK)
                {
                    LogFile.Error("Failed to set device rts (error " + status.ToString() + ")");
                    return false;
                }

                controller = new FtdiDmxController(ftdi, deviceInfo.Description == "DMX USB PRO");
                return true;
            }
            catch (Exception e)
            {
                LogFile.Error(e, "Error occurred while accessing dmx device: ");
                return false;
            }
        }

        public void Dispose()
        {
            if (ftdi.IsOpen)
                ftdi.Close();
        }
    }
}
