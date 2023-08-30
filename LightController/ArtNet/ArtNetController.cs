using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LightController.ArtNet.Packet;

namespace LightController.ArtNet
{
    public class ArtNetController
    {
        private ArtNetSocket socket = new ArtNetSocket();
        private ArtNetDmxPacket packet = new ArtNetDmxPacket();

        public ArtNetController() 
        {
            packet.Data = new byte[512];
        }

        public bool IsOpen => socket.IsOpen;

        public bool TryOpenSocket(string interfaceAddress)
        {
            IPAddress address;
            if (string.IsNullOrWhiteSpace(interfaceAddress) || !IPAddress.TryParse(interfaceAddress, out address))
                address = null;
            OpenSocket(address);
            return socket.IsOpen;
        }
        private void OpenSocket(IPAddress interfaceAddress = null)
        {
            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.SupportsMulticast && adapter.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties ipProperties = adapter.GetIPProperties();

                    foreach (UnicastIPAddressInformation unicast in ipProperties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            if(interfaceAddress == null || unicast.Address == interfaceAddress)
                            {
                                socket.Open(unicast.Address, unicast.IPv4Mask);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public void SetChannels(int startChannel, byte[] values)
        {
            if (startChannel < 1 || startChannel + values.Length > packet.Data.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startChannel), "Start channel number must be between 1 and 512.");
            }

            Buffer.BlockCopy(values, 0, packet.Data, startChannel, values.Length);
        }

        public void WriteData()
        {
            if (socket.IsOpen)
                socket.Send(packet);
        }

    }
}
