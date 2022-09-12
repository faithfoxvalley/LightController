using MediaToolkit.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Pro
{
    public class FfTaskGetThumbnail2 : FfTaskGetThumbnail
    {
        public FfTaskGetThumbnail2(string inputFilePath, GetThumbnailOptions options) : base(inputFilePath, options)
        {
        }

        public override IList<string> CreateArguments()
        {
            var list = base.CreateArguments();
            int seekIndex = list.IndexOf("-ss");
            if(seekIndex >= 0)
            {
                list.RemoveAt(seekIndex);
                list.RemoveAt(seekIndex);
            }
            return list;
        }
    }
}
