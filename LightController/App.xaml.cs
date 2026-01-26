using System.Windows;

namespace LightController;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    void App_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        LightController.MainWindow.Instance?.Shutdown();
    }
}
