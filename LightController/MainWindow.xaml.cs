using LightController.Bacnet;
using LightController.Config;
using LightController.Dmx;
using LightController.Pro;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace LightController;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const int DmxUpdateFps = 30;
    private const int InputsUpdateFps = 20;
    private const int BacnetUpdateFps = 10;
    private const string AppGuid = "a013f8c4-0875-49e3-b671-f4e16a1f1fe4";
    private const int AcquireMutexTimeout = 1000;
    private string DefaultShowFile => Path.Combine(ApplicationData, "default.show");

    private ProPresenter pro;
    private readonly ConfigFile mainConfig;
    private readonly ShowConfig showConfig;
    private SceneManager sceneManager;
    private DmxProcessor dmx;
    private BacnetProcessor bacNet;
    private TickLoop inputsTimer; // Runs on different thread
    private TickLoop bacNetTimer; // Runs on different thread
    private bool inputActivated = false;

    private static Mutex mutex;
    private static bool mutexActive;

    private System.Windows.Threading.DispatcherTimer uiTimer;
    private PreviewWindow preview;

    public static MainWindow Instance { get; private set; }

    public string ApplicationData { get; private set; }
    public ProPresenter Pro => pro;

    public MainWindow()
    {
        Instance = this;

        InitializeComponent();

        InitAppData();

        if (!IsOnlyInstance())
        {
            ErrorBox.Show("Error: The lighting controller is already running!");
            return;
        }


        ClockTime.Init();

        CommandLineOptions args = new CommandLineOptions(Environment.GetCommandLineArgs());
        string command = args.ToString();
        if (!string.IsNullOrWhiteSpace(command))
            Log.Info("Command: " + command);

        try
        {
            mainConfig = ConfigFile.Load(Path.Combine(ApplicationData, "config.yml"));
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occurred while reading the main config file!");
            ErrorBox.Show("An error occurred while reading the main config file, please check your config.");
        }

        try
        {
            if (ShowConfig.TryGetFilePathFromArgs(args, out string showFile))
            {
                showLabel.Content = showFile;
                showLabel.ToolTip = showFile;
            }
            else
                showFile = DefaultShowFile;
            showConfig = ShowConfig.Load(showFile);
        }
        catch (Exception e)
        {
            Log.Error(e, "An error occurred while reading the show file!");
            ErrorBox.Show("An error occurred while reading the show file, please check your show config.");
        }

        pro = new ProPresenter(mainConfig.ProPresenter, mediaList);
        dmx = new DmxProcessor(mainConfig.Dmx, DmxUpdateFps);
        bacNet = new BacnetProcessor(showConfig.Bacnet, bacnetList);
        if (bacNet.Enabled)
            bacnetContainer.Visibility = Visibility.Visible;

        string defaultScene = showConfig.DefaultScene;
        if(args.TryGetFlagArg("scene", 0, out string sceneFlag) && showConfig.Scenes.Any(x => x.Name == sceneFlag.Trim()))
            defaultScene = sceneFlag;
        sceneManager = new SceneManager(showConfig.Scenes, mainConfig.MidiDevice, defaultScene, dmx, showConfig.DefaultTransitionTime, sceneList, bacNet);

        // Update fixture list
        dmx.AppendToListbox(fixtureList);

        inputsTimer = new TickLoopAsync(InputsUpdateFps, UpdateInputs);
        if (bacNet.Enabled)
            bacNetTimer = new TickLoop(BacnetUpdateFps, UpdateBacnet);

        uiTimer = new System.Windows.Threading.DispatcherTimer();
        uiTimer.Interval = new TimeSpan(0, 0, 1);
        uiTimer.Tick += UiTimer_Tick;
        uiTimer.Start();

        if (args.HasFlag("preview"))
            OpenPreviewClick(null, null);

        Activate();
    }

    private static bool IsOnlyInstance()
    {
        mutex = new Mutex(true, AppGuid, out mutexActive);
        if (!mutexActive)
        {
            try
            {
                mutexActive = mutex.WaitOne(AcquireMutexTimeout);
                if (!mutexActive)
                    return false;
            }
            catch (AbandonedMutexException)
            { } // Abandoned probably means that the process was killed or crashed
        }

        return true;
    }

    private void UiTimer_Tick(object sender, EventArgs e)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("[Dmx]").AppendLine();
        dmx.AppendPerformanceInfo(sb);
        sb.Append("[Input]").AppendLine();
        inputsTimer.AppendPerformanceInfo(sb);
        if(bacNetTimer != null)
        {
            sb.AppendLine();
            sb.Append("[Bacnet]").AppendLine();
            bacNetTimer.AppendPerformanceInfo(sb);
        }
        performanceInfo.Text = sb.ToString();
    }

    private void InitAppData()
    {
        AssemblyName mainAssemblyName = typeof(MainWindow).Assembly.GetName();
        string appname = mainAssemblyName.Name;
        string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException("No /AppData/Local/ folder exists!");
        path = Path.Combine(path, appname);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        ApplicationData = path;

        Log.Init(Path.Combine(ApplicationData, "Logs", appname + ".log"));
        if(mainAssemblyName.Version != null)
            Log.Info("Started application - v" + mainAssemblyName.Version.ToString(3));
        else
            Log.Info("Started application");
        Title = "Light Controller - v" + mainAssemblyName.Version.ToString(3);
    }

    // This runs on a different thread
    private async Task UpdateInputs()
    {
        if (!inputActivated)
        {
            await sceneManager.ActivateSceneAsync();
            inputActivated = true;
        }

        await sceneManager.UpdateAsync();
    }

    // This runs on a different thread
    private void UpdateBacnet()
    {
        bacNet.Update();
    }

    private void Restart(string showFile = null)
    {
        if (showFile == null)
            showFile = showConfig.FileLocation;

        Log.Info("Restarting application");
        string currentScene = sceneManager?.ActiveSceneName;
        string fileName = Process.GetCurrentProcess().MainModule.FileName;
        StringBuilder sb = new StringBuilder();

        if (currentScene != null && !currentScene.Contains('"'))
        {
            sb.Append("-scene ");
            if (currentScene.Contains(' '))
                sb.Append('"').Append(currentScene).Append('"');
            else
                sb.Append(currentScene);
        }

        if (showFile != DefaultShowFile)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append("-config ");
            if (showFile.Contains(' '))
                sb.Append('"').Append(showFile).Append('"');
            else
                sb.Append(showFile);
        }

        if (preview != null)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append("-preview");
        }

        if (sb.Length > 0)
            Process.Start(fileName, sb.ToString());
        else
            Process.Start(fileName);
        Process.GetCurrentProcess().Kill();
    }

    #region Application Menu
    private void RestartClick(object sender, RoutedEventArgs e)
    {
        Restart();
    }

    private void ShowLogsClick(object sender, RoutedEventArgs e)
    {
        string logs = Path.Combine(ApplicationData, "Logs");
        if (Directory.Exists(logs))
            Process.Start("explorer.exe", "\"" + logs + "\"");
    }

    private void DebugDmxClick(object sender, RoutedEventArgs e)
    {
        dmx.WriteDebug();
    }

    private void OpenPreviewClick(object sender, RoutedEventArgs e)
    {
        if (preview == null)
        {
            preview = new PreviewWindow();
            preview.Loaded += (o, e) =>
            {
                dmx.InitPreview(preview);
            };

        }
        preview.Show();
        preview.Closing += (o, e) =>
        {
            dmx.ClosePreview();
            preview = null;
        };
    }

    private void EditConfigClick(object sender, RoutedEventArgs e)
    {
        mainConfig.Open();
    }
    #endregion

    #region Show Menu
    private void EditShowClick(object sender, RoutedEventArgs e)
    {
        showConfig.Open();
    }

    private void LoadShowClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = ShowConfig.CreateOpenDialog();
        if (openFileDialog.ShowDialog() == true && File.Exists(openFileDialog.FileName))
        {
            try
            {
                ShowConfig.Load(openFileDialog.FileName);
                Restart(openFileDialog.FileName);   
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error while test loading show config!");
                ErrorBox.Show("An error occurred while reading the show file!", false);
            }
        }
    }

    private void LoadDefaultShowClick(object sender, RoutedEventArgs e)
    {
        Restart(DefaultShowFile);   
    }

    private void SaveDefaultShowClick(object sender, RoutedEventArgs e)
    {
        try
        {
            showConfig.Save(DefaultShowFile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while saving the show file!");
            ErrorBox.Show("An error occurred while saving the show file!", false);
        }
    }

    private void SaveAsShowClick(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = ShowConfig.CreateSaveDialog();
        if (saveFileDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(saveFileDialog.FileName))
        {
            try
            {
                showConfig.Save(saveFileDialog.FileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while saving the show file!");
                ErrorBox.Show("An error occurred while saving the show file!", false);
            }
        }
    }
    #endregion


    private void ListBox_DisableMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Disables selection
        e.Handled = true;
    }


    private void Window_Closed(object sender, EventArgs e)
    {
        if (dmx == null)
            return;


        inputsTimer.Dispose();
        bacNetTimer?.Dispose();
        sceneManager.Disable();

        Shutdown();

        Log.Info("Closed application.");
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
#if !DEBUG
        MessageBoxResult messageBoxResult = MessageBox.Show("Are you sure you want to exit?", "Exit Confirmation", MessageBoxButton.YesNo);
        if (messageBoxResult != MessageBoxResult.Yes)
            e.Cancel = true;
#endif
        if (preview != null)
            preview.Close();
    }

    public void Shutdown()
    {
        dmx.TurnOff();
        if(mutexActive)
            mutex.ReleaseMutex();
    }

}
