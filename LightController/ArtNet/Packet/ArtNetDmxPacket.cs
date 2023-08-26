using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.ArtNet.Packet
{
    public class ArtNetDmxPacket : ArtNetPacket
    {
        public ArtNetDmxPacket() : base(ArtNetOpCodes.Dmx)
        {
        }

        public byte Sequence { get; set; }
        public byte Physical { get; set; }
        public ushort Universe { get; set; }
        public byte[] Data { get; set; }

        protected override void WriteTo(BinaryWriter writer)
        {
            base.WriteTo(writer);
            writer.Write(Sequence);
            writer.Write(Physical);
            writer.Write(ToLittleEndian(Universe));
            writer.Write(ToBigEndian((ushort)Data.Length));
            writer.Write(Data);
        }
    }
}
