using LightController.ArtNet.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LightController.ArtNet
{
    public class ArtNetPacketEventArgs
    {
        public ArtNetPacketEventArgs(IPEndPoint source, ArtNetPacket packet)
        {
            Source = source;
            Packet = packet;
        }

        public IPEndPoint Source { get; set; }
        public ArtNetPacket Packet { get; set; }
    }
}
