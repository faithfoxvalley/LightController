using LightController.Color;
using LightController.Config;
using LightController.Config.Input;
using LightController.Pro;
using MediaToolkit.Services;
using MediaToolkit.Tasks;
using NAudio.Midi;
using System;
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

        public MainWindow()
        {
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

        }

        private async void btnInit_Click(object sender, RoutedEventArgs e)
        {
            pro = new ProPresenter("http://localhost:1025/v1/");
            await pro.AsyncInit();
            ProLibrary library = pro.Libraries.First();
            await pro.AsyncUpdateLibraryData(library);

            ConfigFile config = ConfigFile.Load();
            config.Inputs.Add(new ColorInput(new ColorRGB(255, 0, 128), new ValueRange(1, 5)));
            config.Inputs.Add(new ColorInput(new ColorRGB(50, 255, 50), new ValueRange(6, 6)));
            config.Inputs.Add(new ProPresenterInput(pro, new ValueRange(7, 20)));
            config.Save();

            if (comboBoxMidiInDevices.SelectedIndex >= 0)
            {
                midiIn = new MidiIn(comboBoxMidiInDevices.SelectedIndex);
                midiIn.MessageReceived += midiIn_MessageReceived;
                midiIn.ErrorReceived += midiIn_ErrorReceived;
                midiIn.Start();
            }

            MessageBox.Show("Done!");
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
            foreach(InputBase input in config.Inputs)
            {

            }
        }
        
    }
}
