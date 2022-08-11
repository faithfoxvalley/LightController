using LightController.Color;
using System.Windows;

namespace LightController.Config.Input
{
    //[YamlTag("!debug_input")]
    public class DebugInput : InputBase
    {
        private const string pickerName = "colorPicker";

        private ColorPicker.StandardColorPicker picker;
        private ColorHSV hsv = new ColorHSV(0, 1, 1);
        private double pickerIntensity = 1;

        public override void Init()
        {
            foreach (Window window in App.Current.Windows)
            {
                picker = window.FindName(pickerName) as ColorPicker.StandardColorPicker;
                if (picker != null)
                {
                    picker.ColorChanged += Picker_ColorChanged;
                    break;
                }
            }
        }

        private void Picker_ColorChanged(object sender, RoutedEventArgs e)
        {
            hsv = (ColorHSV)ColorRGB.FromColor(picker.SelectedColor);
            pickerIntensity = picker.SelectedColor.A / 255d;
        }

        public override ColorHSV GetColor(int fixtureId)
        {
            return hsv;
        }

        public override double GetIntensity(int fixtureId, ColorHSV target)
        {
            return pickerIntensity * base.GetIntensity(fixtureId, target);
        }
    }
}
