using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

namespace LightController.ArtNet.Packet
{
    public abstract class ArtNetPacket
    {
        protected ArtNetOpCodes opCode;
        
        private const string Protocol = "Art-Net";
        private const ushort ProtocolVersion = 14;
        private static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

        public ArtNetOpCodes OpCode => opCode;

        protected ArtNetPacket(ArtNetOpCodes opCode)
        {
            this.opCode = opCode;
        }

        protected virtual void WriteTo(BinaryWriter writer)
        {
            writer.Write(Encoding.ASCII.GetBytes(Protocol));
            writer.Write((byte)0);
            writer.Write(ToLittleEndian((ushort)opCode));
            writer.Write(ToBigEndian(ProtocolVersion));
        }

        protected virtual void ReadFrom(BinaryReader reader)
        {
        }

        protected static ushort ToLittleEndian(ushort value)
        {
            return IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
        }

        protected static ushort ToBigEndian(ushort value)
        {
            return IsLittleEndian ? BinaryPrimitives.ReverseEndianness(value) : value;
        }

        public byte[] ToArray()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                WriteTo(writer);

                return stream.ToArray();
            }
        }

        public static ArtNetPacket CreatePacket(byte[] data)
        {
            using (MemoryStream stream = new MemoryStream(data))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                ArtNetPacket packet = CreatePacket(ReadOpCode(reader));
                packet?.ReadFrom(reader);
                return packet;
            }
        }

        private static ArtNetPacket CreatePacket(ArtNetOpCodes opCode)
        {
            switch (opCode)
            {
                case ArtNetOpCodes.Dmx:
                    return new ArtNetDmxPacket();
                case ArtNetOpCodes.None:
                    return null;
                default:
                    return new ArtNetUnkownPacket(opCode);
            }
        }

        private static ArtNetOpCodes ReadOpCode(BinaryReader reader)
        {
            try
            {
                string protocol = Encoding.ASCII.GetString(reader.ReadBytes(Protocol.Length));
                if (protocol != Protocol)
                    return ArtNetOpCodes.None;
                reader.ReadByte();
                ushort opCode = reader.ReadUInt16();
                if (Enum.IsDefined(typeof(ArtNetOpCodes), opCode))
                    return (ArtNetOpCodes)opCode;
            }
            catch
            { }
            return ArtNetOpCodes.None;
        }
    }
}
