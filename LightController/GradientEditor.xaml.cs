using LightController.Color;
using LightController.Config;
using LightController.Config.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LightController
{
    /// <summary>
    /// Interaction logic for GradientEditor.xaml
    /// </summary>
    public partial class GradientEditor : Window
    {
        private List<GradientInputFrame> gradient;
        private GradientInputFrame currentFrame;
        private bool spaceEvenly;
        private double scale = 1;
        private static readonly Regex numericFilter = new Regex("[^0-9.]+");
        private WriteableBitmap canvasImage;

        public GradientEditor(GradientInput gradient)
        {
            InitializeComponent();

            this.gradient = gradient.Colors ?? new List<GradientInputFrame>();
            foreach(GradientInputFrame frame in gradient.Colors)
                frameList.Items.Add("-");
            spaceEvenly = gradient.SpaceEvenly;
            scale = gradient.Scale;

            this.Loaded += GradientEditor_Loaded;
        }

        private void GradientEditor_Loaded(object sender, RoutedEventArgs e)
        {
            scaleTextBox.Text = scale.ToString();

            Size size = new Size(100, 20);//GetElementPixelSize(previewCanvas);
            int width = (int)Math.Ceiling(size.Width);
            int height = (int)Math.Ceiling(size.Height);
            canvasImage = BitmapFactory.New(width, height);
            previewCanvas.Source = canvasImage;
            UpdateCanvas();
        }

        private void FrameSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (frameList.SelectedIndex < 0 || frameList.SelectedIndex >= gradient.Count)
            {
                currentFrame = null;
                return;
            }

            currentFrame = gradient[frameList.SelectedIndex];

            ColorHSV hsv = currentFrame.Color;
            var state = new ColorPicker.Models.ColorState()
            {
                HSV_H = hsv.Hue,
                HSV_V = hsv.Value,
                HSV_S = hsv.Saturation,
                A = 1,
            };
            frameColorPicker.ColorState = state;

            frameLocationSlider.Value = currentFrame.Location * 100;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            currentFrame = new GradientInputFrame();
            gradient.Add(currentFrame);
            frameList.Items.Add("-");
            UpdateCanvas();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (frameList.SelectedIndex < 0 || frameList.SelectedIndex >= gradient.Count)
                return;
            gradient.RemoveAt(frameList.SelectedIndex);
            frameList.Items.RemoveAt(frameList.SelectedIndex);
            currentFrame = null;
            UpdateCanvas();
        }

        private void FilterNumericTextbox(object sender, TextCompositionEventArgs e)
        {
            if(!IsTextAllowed(e.Text))
                e.Handled = true;
        }
        private void FilterNumericTextbox(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
        private bool IsTextAllowed(string text)
        {
            return !numericFilter.IsMatch(text);
        }

        private void ScaleTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!double.TryParse(scaleTextBox.Text, out double scale))
                scale = 1;
            this.scale = scale;
            UpdateCanvas();
        }

        private void ColorChanged(object sender, RoutedEventArgs e)
        {
            if (currentFrame == null)
                return;

            currentFrame.Color = ColorHSV.FromColor(frameColorPicker.ColorState);
            UpdateCanvas();
        }


        public GradientInput BuildInput()
        {
            return new GradientInput()
            {
                Colors = gradient,
                SpaceEvenly = spaceEvenly,
                Scale = scale,
            };
        }


        private void UpdateCanvas()
        {
            if(gradient == null || gradient.Count == 0 || canvasImage == null)
            {
                if(previewCanvas != null)
                    previewCanvas.Visibility = Visibility.Hidden;
                return;
            }

            GradientInput.Iterator iterator = BuildInput().ColorIterator;
            int pixelWidth = canvasImage.PixelWidth;

            ColorRGB[] colors = new ColorRGB[pixelWidth];
            for (int x = 0; x < pixelWidth; x++)
                colors[x] = (ColorRGB)iterator.GetColor(x / (double)pixelWidth);

            using (canvasImage.GetBitmapContext())
            {
                canvasImage.ForEach((x, y, c) =>
                {
                    ColorRGB rgb = colors[x];
                    return new System.Windows.Media.Color()
                    {
                        R = rgb.Red,
                        G = rgb.Green,
                        B = rgb.Blue,
                        ScA = (float)rgb.Value,
                    };
                });
            }

            previewCanvas.Visibility = Visibility.Visible;
        }

        public Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            PresentationSource presentationSource = PresentationSource.FromVisual(element);
            if (presentationSource != null)
                transformToDevice = presentationSource.CompositionTarget.TransformToDevice;
            else
                using (HwndSource hwndSource = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = hwndSource.CompositionTarget.TransformToDevice;

            if (element.DesiredSize == new Size())
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return (Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        private void FrameLocationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentFrame == null)
                return;

            currentFrame.Location = frameLocationSlider.Value / 100.0;
            gradient.Sort(new FrameComparer());
            UpdateCanvas();
        }

        private class FrameComparer : IComparer<GradientInputFrame>
        {
            public int Compare(GradientInputFrame x, GradientInputFrame y)
            {
                return x.Location.CompareTo(y.Location);
            }
        }
    }
}
