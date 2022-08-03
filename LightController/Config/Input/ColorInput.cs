using LightController.Color;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    [YamlTag("!color_input")]
    public class ColorInput : InputBase
    {
        [YamlMember(Alias = "rgb", ApplyNamingConventions = false)]
        public ColorRGB RGB { get; set; }

        public ColorInput() { }

        public override ColorRGB GetColor(int fixtureId)
        {
            return RGB;
        }
    }
}
