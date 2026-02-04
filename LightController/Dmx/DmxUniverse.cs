using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace LightController.Dmx;

internal class DmxUniverse
{
    private bool debug;
    private List<DmxFixture> fixtures = new List<DmxFixture>();
    private IDmxController controller = new NullDmxController();
    private readonly TickLoop dmxTimer;
    private string name;
    private PreviewWindow preview;

    public DmxUniverse(string serialNumber, int fps, bool optional)
    {
        while (!OpenDevice(serialNumber) && !optional)
        {
#if DEBUG
            if(string.IsNullOrWhiteSpace(serialNumber))
                Log.Error($"No DMX Devices found");
            else
                Log.Error($"DMX Device {serialNumber} not found");
            break;
#else
            if (string.IsNullOrWhiteSpace(serialNumber))
                ErrorBox.ExitOnCancel($"No DMX Devices found. Press OK to try again or Cancel to exit.");
            else
                ErrorBox.ExitOnCancel($"DMX Device {serialNumber} not found. Press OK to try again or Cancel to exit."); 
#endif
        }

        dmxTimer = new TickLoop(fps, Write);
        if (!string.IsNullOrWhiteSpace(serialNumber) && name == null)
            name = serialNumber;
    }

    private bool OpenDevice(string serialNumber)
    {

        if (FtdiDmxController.TryOpenDevice(serialNumber, out IDmxController controller))
        {
            this.controller = controller;
            this.name = controller.Name;
            return true;
        }
        this.controller = new NullDmxController();
        this.name = null;
        return false;
    }


    public void Write()
    {
#if !DEBUG
        if (!controller.IsOpen)
            return;
#endif
        Dictionary<int, DmxFrame> frames = null;
        if (this.preview != null)
            frames = new Dictionary<int, DmxFrame>();

        foreach (DmxFixture fixture in fixtures)
        {
            DmxFrame frame = fixture.GetFrame();
            controller.SetChannels(frame.StartAddress, frame.Data);
            if (frames != null)
                frames[fixture.FixtureId] = frame;
        }

        PreviewWindow preview = this.preview;
        if (preview != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (DmxFixture fixture in fixtures)
                {
                    DmxFrame frame = frames[fixture.FixtureId];
                    preview.SetPreviewColor(fixture.FixtureId, frame.PreviewColor, frame.PreviewIntensity);
                }
            });
        }

        if (debug)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DMX data for ").Append(controller.Name).Append(':').AppendLine();
            controller.WriteDebugInfo(sb, 8);
            debug = false;
            Log.Info(sb.ToString());
        }

        if (controller.IsOpen)
            controller.WriteData();
    }


    public void TurnOff()
    {
        Log.Info("Turning off " + controller.Name);

        if (controller.IsOpen)
            controller.Dispose();
    }


    public void WriteDebug()
    {
        debug = true;
    }

    internal void AddFixture(DmxFixture fixture)
    {
        fixtures.Add(fixture);
    }

    internal void AppendPerformanceInfo(StringBuilder sb)
    {
        if(name != null)
            sb.Append(name).AppendLine();
        dmxTimer.AppendPerformanceInfo(sb);
        sb.AppendLine();
    }

    internal void InitPreview(PreviewWindow preview)
    {
        this.preview = preview;
    }

    internal void ClosePreview()
    {
        preview = null;
    }
}
