using LightController.Color;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Pro
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class MediaFrame
    {
        [ProtoMember(1)]
        private ColorRGB[] data;
        [ProtoMember(2)]
        private double time;


        public MediaFrame() { }
    }
}
