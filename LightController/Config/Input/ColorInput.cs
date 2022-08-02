using Colourful;
using LightController.Color;
using YamlDotNet.Serialization;

namespace LightController.Config.Input
{
    [YamlTag("!color_input")]
    public class ColorInput : InputBase
    {
        private RGBColor rgb;

        [YamlMember(Alias = "rgb", ApplyNamingConventions = false)]
        public ColorRGB RGB
        {
            get
            {
                rgb.ToRGB8Bit(out byte r, out byte g, out byte b);
                return new ColorRGB(r, g, b);
            }
            set
            {
                rgb = new RGBColor(value.Red / 255d, value.Green / 255d, value.Blue / 255d);
            }
        }

        public ColorInput() { }

        public override RGBColor GetColor()
        {
            return rgb;
        }
    }
}
