using LightController.Color;
using LightController.Config;
using LightController.Config.Input;
using LightController.Dmx;
using LightController.Pro;
using MediaToolkit.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace LightController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string FfmpegFilePath = @"C:\Bin\ffmpeg.exe";
        private const int DmxUpdateRate = 40;
        private const int InputsUpdateRate = 200;

        private ProPresenter pro;
        private IMediaToolkitService ffmpeg;
        private ConfigFile config;
        private SceneManager sceneManager;
        private DmxProcessor dmx;
        private Timer dmxTimer; // Runs on different thread
        private Timer inputsTimer; // Runs on different thread
        private bool inputActivated = false;

        public static MainWindow Instance { get; private set; }

        public string ApplicationData { get; }
        public ProPresenter Pro => pro;
        public IMediaToolkitService Ffmpeg => ffmpeg;

        public MainWindow()
        {
            Instance = this;

            InitializeComponent();

            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("No /AppData/Local/ folder exists!");
            path = Path.Combine(path, typeof(MainWindow).Assembly.GetName().Name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            ApplicationData = path;

            ffmpeg = MediaToolkitService.CreateInstance(FfmpegFilePath);

            config = ConfigFile.Load();

            pro = new ProPresenter(config.ProPresenter);
            dmx = new DmxProcessor(config.Dmx);
            dmx.TurnOff();
            sceneManager = new SceneManager(config.Scenes, config.MidiDevice, config.DefaultScene, dmx, listScene);

            // Update scene combobox
            foreach (var scene in config.Scenes)
                listScene.Items.Add(scene.Name);
            listScene.SelectionChanged += ListScene_SelectionChanged;

            // https://stackoverflow.com/a/12797382
            dmxTimer = new Timer(UpdateDmx, null, DmxUpdateRate, Timeout.Infinite);
            inputsTimer = new Timer(UpdateInputs, null, InputsUpdateRate, Timeout.Infinite);

        }
        private void ListScene_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }

        // This runs on a different thread
        private async void UpdateInputs(object state)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                if (!inputActivated)
                {
                    await sceneManager.ActivateSceneAsync();
                    inputActivated = true;
                }

                await sceneManager.UpdateAsync();

                sw.Stop();
                inputsTimer.Change(Math.Max(0, InputsUpdateRate - sw.ElapsedMilliseconds), Timeout.Infinite);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        // This runs on a different thread
        private void UpdateDmx(object state)
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                dmx.Write();

                sw.Stop();
                dmxTimer.Change(Math.Max(0, DmxUpdateRate - sw.ElapsedMilliseconds), Timeout.Infinite);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            dmxTimer.Change(Timeout.Infinite, Timeout.Infinite);
            dmx.TurnOff();
        }

        /*private async void btnCheckContent_Click(object sender, RoutedEventArgs e)
        {
            /*const int targetMs = 500;
            const double marginTop = 50;
            const double marginLeft = 50;
            const double width = 1;
            const double height = 50;

            Stopwatch sw = new Stopwatch();
            for (int x = 0; x < 300; x++)
            {
                sw.Restart();
                canvas.Children.Clear();

                var status = await pro.AsyncGetTransportStatus(Layer.Presentation);
                if (!status.audio_only && !string.IsNullOrWhiteSpace(status.name) && status.duration > 1)
                {
                    string path = System.IO.Path.Combine(Media, status.name);
                    if (File.Exists(path))
                    {
                        double time = await pro.AsyncGetTransportLayerTime(Layer.Presentation);
                        time = Math.Min(Math.Round(time), status.duration - 1);

                        GetThumbnailOptions options = new GetThumbnailOptions
                        {
                            SeekSpan = TimeSpan.FromSeconds(time),
                            OutputFormat = OutputFormat.Image2,
                            PixelFormat = MediaToolkit.Tasks.PixelFormat.Rgba,
                            FrameSize = new FrameSize(854, 480)
                        };

                        GetThumbnailResult result = await service.ExecuteAsync(new FfTaskGetThumbnail(
                          path,
                          options
                        ));

                        string filePath = @"C:\Users\austi\Desktop\Test\image.jpg";
                        if (!File.Exists(filePath))
                        {
                            try
                            {
                                await File.WriteAllBytesAsync(filePath, result.ThumbnailData);
                            }
                            catch { }
                        }

                        if (result.ThumbnailData.Length > 0)
                        {
                            ColorRGB[] colorData = await Task.Run(() => MediaLibrary.ReadImage(result.ThumbnailData, 14, 1));
                            for (int i = 0; i < colorData.Length; i++)
                            {
                                ColorRGB color = colorData[i];
                                var winColor = System.Windows.Media.Color.FromRgb(color.Red, color.Green, color.Blue);
                                var rect = new Rectangle
                                {
                                    Fill = new SolidColorBrush(winColor),
                                    Width = width,
                                    Height = height,
                                };
                                Canvas.SetTop(rect, marginTop);
                                Canvas.SetLeft(rect, marginLeft + (i * width));
                                canvas.Children.Add(rect);
                            }
                        }
                        label.Content = $"Time: {time}s took {sw.ElapsedMilliseconds}ms";
                    }
                    else
                    {
                        label.Content = "File does not exist!";
                    }
                }
                else
                {
                    label.Content = "Bad response from ProPresenter";
                }

                sw.Stop();
                long ms = targetMs - sw.ElapsedMilliseconds;
                if (ms > 0)
                    await Task.Delay((int)ms);
                break;
            }
            
        }*/
    }
}
