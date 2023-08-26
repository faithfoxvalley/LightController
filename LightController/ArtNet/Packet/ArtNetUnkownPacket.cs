using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.ArtNet.Packet
{
    public class ArtNetUnkownPacket : ArtNetPacket
    {
        public ArtNetUnkownPacket(ArtNetOpCodes opCode) : base(opCode)
        {
        }
    }
}
