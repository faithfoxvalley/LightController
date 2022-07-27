using LightController.Color;

namespace LightController.Config.Input
{
    [YamlTag("!color_input")]
    public class ColorInput : InputBase
    {
        public Color.ColorRGB Color { get; set; }

        public ColorInput() { }

        public ColorInput(ColorRGB color, ValueRange channels) : base(channels)
        {
            Color = color;
        }
    }
}
