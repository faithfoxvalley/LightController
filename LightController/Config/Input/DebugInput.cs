using LightController.Color;
using LightController.Midi;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LightController.Config.Input
{
    [YamlTag("!debug_input")]
    public class DebugInput : InputBase
    {
        private ColorPicker.DualPickerControlBase picker;
        private ColorHSV hsv = new ColorHSV(0, 0, 1);
        private double pickerIntensity = 1;

        public override void Init()
        {
            picker = MainWindow.Instance.colorPicker;
            if (picker != null)
            {
                picker.ColorChanged += Picker_ColorChanged;
                picker.Visibility = Visibility.Collapsed;
                picker.SelectedColor = new System.Windows.Media.Color
                {
                    R = 255,
                    G = 255,
                    B = 255,
                    A = 255
                };
            }
        }

        public override async Task StartAsync(MidiNote note, CancellationToken cancelToken)
        {
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                picker.Visibility = Visibility.Visible;
            });
        }

        public override async Task StopAsync(CancellationToken cancelToken)
        {
            await Application.Current.Dispatcher.BeginInvoke(() =>
            {
                picker.Visibility = Visibility.Collapsed;
            });
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
