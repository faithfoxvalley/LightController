using YamlDotNet.Serialization;

namespace LightController.Config
{
    public class ProPresenterConfig
    {
        [YamlMember(Description = "IP and port from ProPresenter network settings in this form: http://ip-address:port/v1/")]
        public string ApiUrl { get; set; }
        
        [YamlMember(Description = "Path to the media assets folder")]
        public string MediaAssetsPath { get; set; }
    }
}
