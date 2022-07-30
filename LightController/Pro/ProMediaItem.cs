using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Pro
{
    [ProtoContract]
    public class ProMediaItem
    {
        private string path;

        private ProMediaItem() { }

        public ProMediaItem(string path)
        {
            this.path = path;
            
        }

        [ProtoContract]
        public class Frame
        {

        }
    }
}
