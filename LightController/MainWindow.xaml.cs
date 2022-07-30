using Colourful;
using LightController.Color;
using LightController.Config;
using LightController.Config.Input;
using LightController.Pro;
using MediaToolkit.Services;
using MediaToolkit.Tasks;
using NAudio.Midi;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace LightController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ffmpegFilePath = @"C:\Bin\ffmpeg.exe";
        private const string Url = "http://localhost:1025/v1/";
        private ProPresenter pro;
        private MidiIn midiIn;
        private IMediaToolkitService service;
        private ConfigFile config;
        private ColorHSI testHsi = new ColorHSI();

        public static string ApplicationData { get; private set; }

        public object RGBColor { get; }

        public MainWindow()
        {
            ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(ApplicationData))
                throw new DirectoryNotFoundException("No /AppData/Local/ folder exists!");

            InitializeComponent();

            for (int device = 0; device < MidiIn.NumberOfDevices; device++)
            {
                comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
            }
            if (comboBoxMidiInDevices.Items.Count > 0)
            {
                comboBoxMidiInDevices.SelectedIndex = 0;
            }

            service = MediaToolkitService.CreateInstance(ffmpegFilePath);

            config = ConfigFile.Load();
            config.Init();
        }

        private void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
        }

        private void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
        }

        private async void btnCheckContent_Click(object sender, RoutedEventArgs e)
        {
            var status = await pro.AsyncGetTransportStatus(Layer.Presentation);
            if(status.is_playing && System.IO.Path.HasExtension(status.name))
            {
                string path = @"P:\0 ProPresenter\Backup\Media\Assets\" + status.name;
                if(System.IO.File.Exists(path))
                {
                    GetThumbnailOptions options = new GetThumbnailOptions
                    {
                        SeekSpan = TimeSpan.FromSeconds(10),
                        OutputFormat = OutputFormat.Image2,
                        PixelFormat = MediaToolkit.Tasks.PixelFormat.Rgba
                    };

                    GetThumbnailResult result = await service.ExecuteAsync(new FfTaskGetThumbnail(
                      path,
                      options
                    ));

                    string newFile = @"C:\Users\austin.vaness\Desktop\Test\image.jpg";

                    BitmapProcessing.ReadImage(result.ThumbnailData, 14, 0.1);
                }

            }
            else
            {

            }

            /*var currentPresentation = (await pro.AsyncGetCurrentPresentation()).presentation;
            if(currentPresentation.id.uuid != null)
            {
                ProLibrary library = pro.Libraries.Find(x => x.ContainsPresentation(currentPresentation.id.uuid));
                if (library != null)
                {
                    MessageBox.Show($"Presentation: {currentPresentation.id.name} in library {library.Name}");

                }
            }
            else
            {

            }*/
        }

        
        public void Update()
        {
            //foreach(InputBase input in config.Inputs)
            {

            }
        }

        private void sliderHue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            testHsi.Hue = e.NewValue * 240;
            UpdateLabel(testHsi);
        }

        private void sliderSaturation_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            testHsi.Saturation = e.NewValue;
            UpdateLabel(testHsi);
        }

        private void sliderIntensity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            testHsi.Intensity = e.NewValue;
            UpdateLabel(testHsi);
        }

        private void UpdateLabel(ColorHSI hsi)
        {
            ColorRGB rgb = (ColorRGB)hsi;
            ColorRGBW rgbw = (ColorRGBW)hsi;
            lblColor.Content = $"{rgb.Red}, {rgb.Green}, {rgb.Blue}\n{rgbw.Red}, {rgbw.Green}, {rgbw.Blue}, {rgbw.White}";
        }
    }
}
