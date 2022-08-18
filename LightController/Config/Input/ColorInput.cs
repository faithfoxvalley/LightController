using LightController.Color;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    [YamlTag("!color_input")]
    public class ColorInput : InputBase
    {
        [YamlMember(Alias = "rgb", ApplyNamingConventions = false)]
        public ColorRGB RGB { get; set; }

        private ColorHSV black = new ColorHSV(0, 0, 0);

        public ColorInput() { }

        public override ColorHSV GetColor(int fixtureId)
        {
            if (RGB == null)
                return black;
            return (ColorHSV)RGB;
        }
    }
}
