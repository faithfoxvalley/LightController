using LightController.Config;
using LightController.Dmx;
using LightController.Pro;
using MediaToolkit.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace LightController
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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

        public string ApplicationData { get; private set; }
        public ProPresenter Pro => pro;
        public IMediaToolkitService Ffmpeg => ffmpeg;

        public MainWindow()
        {
            Instance = this;

            InitializeComponent();

            InitAppData();
            InitFfmpeg();

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

        private void InitFfmpeg()
        {
            string appLocation = typeof(MainWindow).Assembly.Location;
            if (string.IsNullOrEmpty(appLocation))
                throw new Exception("Unable to find ffmpeg location");
            string ffmpegPath = Path.Combine(Path.GetDirectoryName(appLocation), "ffmpeg.exe");
            if (!File.Exists(ffmpegPath))
                throw new Exception("Unable to find ffmpeg.exe");
            ffmpeg = MediaToolkitService.CreateInstance(ffmpegPath);
        }

        private void InitAppData()
        {
            string appname = typeof(MainWindow).Assembly.GetName().Name;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("No /AppData/Local/ folder exists!");
            path = Path.Combine(path, appname);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            ApplicationData = path;

            LogFile.Init(Path.Combine(ApplicationData, "Logs", appname + ".log"));
            LogFile.Info("Started application");
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

                try
                {
                    dmx.Write();
                }
                catch (OpenDMX.NET.OpenDMXException ex)
                {
                    LogFile.Error(ex);
                }

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
    }
}
