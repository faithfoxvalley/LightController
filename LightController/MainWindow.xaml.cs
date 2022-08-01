using LightController.Color;
using LightController.Config;
using LightController.Config.Input;
using LightController.Dmx;
using LightController.Pro;
using MediaToolkit.Services;
using MediaToolkit.Tasks;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LightController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ffmpegFilePath = @"C:\Bin\ffmpeg.exe";
        private const string Url = "http://localhost:1025/v1/";
        private const string Media = @"D:\Documents\ProPresenter\Media\Assets";

        private ProPresenter pro;
        private IMediaToolkitService service;
        private ConfigFile config;
        private SceneManager sceneManager;
        private SolidColorBrush[] brushes = new SolidColorBrush[10];

        public static MainWindow Instance { get; private set; }

        public string ApplicationData { get; }


        public MainWindow()
        {
            Instance = this;

            InitializeComponent();

            ApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(ApplicationData))
                throw new DirectoryNotFoundException("No /AppData/Local/ folder exists!");


            service = MediaToolkitService.CreateInstance(ffmpegFilePath);

            //config = ConfigFile.Load();
            //sceneManager = new SceneManager(config.Scenes, config.MidiDevice, config.DefaultScene);
            pro = new ProPresenter();

            box0.Fill = brushes[0] = new SolidColorBrush();
            box1.Fill = brushes[1] = new SolidColorBrush();
            box2.Fill = brushes[2] = new SolidColorBrush();
            box3.Fill = brushes[3] = new SolidColorBrush();
            box4.Fill = brushes[4] = new SolidColorBrush();
            box5.Fill = brushes[5] = new SolidColorBrush();
            box6.Fill = brushes[6] = new SolidColorBrush();
            box7.Fill = brushes[7] = new SolidColorBrush();
            box8.Fill = brushes[8] = new SolidColorBrush();
            box9.Fill = brushes[9] = new SolidColorBrush();

            pro.AsyncInit();

            //MediaLibrary.DrawHistogram(@"C:\Users\austi\Desktop\Test\image.jpg");
        }

        private async void btnCheckContent_Click(object sender, RoutedEventArgs e)
        {
            const int targetMs = 500;
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
                            ColorRGB[] colorData = await Task.Run(() => MediaLibrary.ReadImage(result.ThumbnailData, 854, 1));
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
            
        }

        private Point startPoint;
        private Rectangle rect;

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(canvas);

            rect = new Rectangle
            {
                Stroke = Brushes.LightBlue,
                StrokeThickness = 2
            };
            Canvas.SetLeft(rect, startPoint.X);
            Canvas.SetTop(rect, startPoint.Y);
            canvas.Children.Add(rect);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released || rect == null)
                return;

            var pos = e.GetPosition(canvas);

            var x = Math.Min(pos.X, startPoint.X);
            var y = Math.Min(pos.Y, startPoint.Y);

            var w = Math.Max(pos.X, startPoint.X) - x;
            var h = Math.Max(pos.Y, startPoint.Y) - y;

            rect.Width = w;
            rect.Height = h;

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            rect = null;
        }
    }
}
