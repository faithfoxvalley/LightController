using LightController.Color;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LightController.Config.Input
{
    [YamlTag("!debug_input")]
    public class DebugInput : InputBase
    {
        private const string pickerName = "colorPicker";

        private ColorPicker.StandardColorPicker picker;
        private ColorRGB rgb = new ColorRGB();
        private double intensity = 1;

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
            rgb = ColorRGB.FromColor(picker.SelectedColor);
            intensity = picker.SelectedColor.A / 255d;
        }

        public override ColorRGB GetColor(int fixtureId)
        {
            return rgb;
        }

        public override double GetIntensity(int fixtureId, ColorRGB target)
        {
            return intensity * base.GetIntensity(fixtureId, target);
        }
    }
}
