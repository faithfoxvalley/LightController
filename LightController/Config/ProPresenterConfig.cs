using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightController.Config
{
    public class ProPresenterConfig
    {
        public string ApiUrl { get; set; } = "http://localhost:50001/v1/";
        public string MediaAssetsPath { get; set; }
    }
}
