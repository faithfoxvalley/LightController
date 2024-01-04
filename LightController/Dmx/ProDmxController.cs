using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTTEC.Devices;
using ENTTEC.Devices.Data;
using System.IO.Ports;

namespace LightController.Dmx
{

    public class ProDmxController : IDmxController, IDisposable
    {
        public bool IsOpen => port.IsOpen;

        private byte[] buffer = new byte[25];
        private readonly SerialPort port;
        private bool dataChanged;

        public ProDmxController(string portName) 
        {
            port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
            port.Open();
        }

        public static bool TryOpenDevice(string portName, out ProDmxController controller)
        {
            controller = null;
            return false;
            string[] ports = SerialPort.GetPortNames();

            try
            {
                if (string.IsNullOrWhiteSpace(portName))
                {
                    if (ports.Length == 0)
                        return false;

                    controller = new ProDmxController(ports[0]);
                    return true;
                }

                if (ports.Contains(portName))
                {
                    controller = new ProDmxController(portName);
                    return true;
                }
            }
            catch (Exception e)
            {
                LogFile.Error(e, "Error accessing DMX USB Pro device: ");
            }

            return false;
        }

        public void SetChannel(int channel, byte value)
        {
            if (channel < 1 || channel > 512)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), "Channel number must be between 1 and 512.");
            }

            buffer[channel] = value;
            dataChanged = true;
        }

        public void SetChannels(int startChannel, byte[] values)
        {
            int endChannel = startChannel + values.Length;

            if (startChannel < 1 || endChannel > 512)
            {
                throw new ArgumentOutOfRangeException(nameof(startChannel), "Start channel number must be between 1 and 512.");
            }

            if (endChannel >= buffer.Length)
            {
                byte[] newBuffer = new byte[endChannel];
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                buffer = newBuffer;
            }

            Buffer.BlockCopy(values, 0, buffer, startChannel, values.Length);
            dataChanged = true;
        }

        public void WriteData()
        {
            if (!dataChanged)
                return;

            byte[] widgetParamsRequestData = DmxUsbProUtils.CreatePacketForDevice(DmxUsbProConstants.SEND_DMX_PACKET_REQUEST_LABEL, buffer);
            port.Write(widgetParamsRequestData, 0, widgetParamsRequestData.Length);
            dataChanged = false;
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
            for (int i = 1; i < buffer.Length; i++)
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

        public void Dispose()
        {
            if (port.IsOpen)
                port.Close();
        }
    }
}
