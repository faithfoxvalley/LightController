using System;
using System.IO;
using YamlDotNet.Serialization;

namespace LightController.Config
{
    public class ProPresenterConfig
    {
        [YamlMember(Description = "IP and port from ProPresenter network settings in this form: http://ip-address:port/v1/")]
        public string ApiUrl { get; set; } = "http://localhost:1025/v1/";

        [YamlMember(Description = "Path to the media assets folder")]
        public string MediaAssetsPath { get; set; }

        [YamlMember(Description = "Number of media processors to use at the same time")]
        public int MaxMediaProcessors { get; set; } = 2;

        public ProPresenterConfig()
        {
            try
            {
                MediaAssetsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ProPresenter", "Media", "Assets");
            }
            catch (Exception)
            {
                MediaAssetsPath = null;
            }
        }
    }
}
