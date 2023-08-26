using LightController.ArtNet.Packet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LightController.ArtNet
{
    public class ArtNetSocket : IDisposable
    {
        public const int Port = 0x1936;
        private static readonly IPEndPoint RemoteEndpoint = new IPEndPoint(IPAddress.Any, Port);

        public event EventHandler<ArtNetPacketEventArgs> OnPacketReceived;

        private UdpClient udpClient;
        private IPAddress broadcastAddress;

        public ArtNetSocket()
        {
        }

        private IPAddress GetBroadcastAddress(IPAddress localIP, IPAddress localSubnetMask)
        {
            if (localSubnetMask == null)
                return IPAddress.Broadcast;

            byte[] ipAdressBytes = localIP.GetAddressBytes();
            byte[] subnetMaskBytes = localSubnetMask.GetAddressBytes();

            if (ipAdressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Lengths of IP address and subnet mask do not match.");

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];
            for (int i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAdressBytes[i] | (subnetMaskBytes[i] ^ 255));
            }
            return new IPAddress(broadcastAddress);
        }

        public void Open(IPAddress localIp, IPAddress localSubnetMask)
        {
            broadcastAddress = GetBroadcastAddress(localIp, localSubnetMask);

            IPEndPoint udpEndpoint = new IPEndPoint(localIp, Port);
            udpClient = new UdpClient(udpEndpoint);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.EnableBroadcast = true;
            udpClient.MulticastLoopback = false;
            udpClient.BeginReceive(ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            IPEndPoint remote = RemoteEndpoint;
            byte[] data = udpClient.EndReceive(ar, ref remote);
            udpClient.BeginReceive(ReceiveCallback, null);
            try
            {
                ArtNetPacket packet = ArtNetPacket.CreatePacket(data);
                if (packet != null)
                    OnPacketReceived?.Invoke(this, new ArtNetPacketEventArgs(remote, packet));
            }
            catch (Exception ex) 
            {
                LogFile.Error(ex, "An error occurred while reading dmx packets");
            }
        }

        public void Send(ArtNetPacket packet)
        {
            SendTo(packet.ToArray(), new IPEndPoint(broadcastAddress, Port));
        }

        public void Send(ArtNetPacket packet, IPAddress address)
        {
            SendTo(packet.ToArray(), new IPEndPoint(address, Port));
        }

        private void SendTo(byte[] bytes, IPEndPoint endPoint)
        {
            udpClient?.Send(bytes, endPoint);
        }

        public void Dispose()
        {
            udpClient?.Dispose();
        }
    }
}
