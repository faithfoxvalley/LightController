using System.Diagnostics;
using System.Windows;

namespace LightController;

public static class ErrorBox
{
    public static void Show(string msg, bool kill = true)
    {
        MessageBox.Show(msg, "Light Controller", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
        if(kill)
        {
            Log.Error("Application closed: '" + msg + "'");
            Process.GetCurrentProcess().Kill();
        }
    }

    /// <summary>
    /// Closes the application if the user presses cancel
    /// </summary>
    public static void ExitOnCancel(string msg)
    {
        var result = MessageBox.Show(msg, "Light Controller", MessageBoxButton.OKCancel, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.DefaultDesktopOnly);
        if(result != MessageBoxResult.OK)
        {
            Log.Error("Application closed after user prompt: '" + msg + "'");
            Process.GetCurrentProcess().Kill();
        }
    }
}
