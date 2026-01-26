using LightController.Color;
using LightController.Pro;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LightController.Config.Input;

[YamlTag("!propresenter_input")]
public class ProPresenterInput : InputBase
{
    private const int UpdateRate = 20;

    private int runtime = int.MinValue;
    private double transportLayerTime;
    private DateTime lastUpdateTime;

    private ProPresenter pro = null;
    private ProMediaItem media;
    private ColorRGB[] colors;
    private byte maxColorValue;
    private byte minColorValue;
    private int pixelWidth;
    private Percent saturation = new Percent(1);
    private object colorLock = new object();
    private InputIntensity minIntensity = new InputIntensity(0);
    private Dictionary<int, int> idToIndex = new Dictionary<int, int>();


    public bool HasMotion { get; set; } = true;

    public string MinIntensity
    {
        get
        {
            return minIntensity.ToString();
        }
        set
        {
            minIntensity = InputIntensity.Parse(value, 0);
        }
    }

    public string Saturation
    {
        get => saturation.ToString();
        set => saturation = Percent.Parse(value, 1);
    }

    public ProPresenterInput() { }


    public override void Init()
    {
        pro = MainWindow.Instance.Pro;

        int count = 0;
        foreach(int id in FixtureIds.EnumerateValues())
        {
            idToIndex[id] = count;
            count++;
        }
        pixelWidth = count;
    }


    public override async Task StartAsync(Midi.MidiNote note, CancellationToken cancelToken)
    {
        runtime = int.MinValue;

        int? id = null;
        if (note != null && note.Intensity.HasValue && note.Intensity.Value > 0)
            id = note.Intensity.Value;

        // Initialize info about current background

        Progress<double> progress = new Progress<double>();
        progress.ProgressChanged += ReportMediaProgress;
        ReportMediaProgress(null, double.NaN);

        try
        {
            Stopwatch sw = Stopwatch.StartNew();
            ProMediaItem newMedia = await pro.GetCurrentMediaAsync(HasMotion, progress, cancelToken, id);
            media = newMedia;
            transportLayerTime = 0;
            lastUpdateTime = DateTime.Now;

            lock (colorLock)
            {
                colors = media.GetData(pixelWidth, transportLayerTime);
                media.GetColorValueBounds(pixelWidth, out maxColorValue, out minColorValue);
            }
            Log.Info($"{(HasMotion ? "Media" : "Thumbnail")} generation took {sw.ElapsedMilliseconds}ms");
        }
        catch (JsonException e)
        {
            Log.Error(e, "Unable to communicate with ProPresenter:");
        }
        catch (HttpRequestException e)
        {
            Log.Error(e, "Unable to communicate with ProPresenter:");
        }
        catch (OperationCanceledException)
        {
            Log.Info($"Canceled {(HasMotion ? "media" : "thumbnail")} generation");
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        ReportMediaProgress(null, 0);


    }

    public override Task StopAsync(CancellationToken cancelToken)
    {
        return pro.DeselectMediaItem();
    }

    private void ReportMediaProgress(object sender, double percent)
    {
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            if (double.IsNaN(percent))
            {
                MainWindow.Instance.mediaProgress.IsIndeterminate = true;
                MainWindow.Instance.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
            }
            else
            {
                MainWindow.Instance.mediaProgress.IsIndeterminate = false;
                MainWindow.Instance.mediaProgress.Value = percent;
                System.Windows.Shell.TaskbarItemInfo taskbarItemInfo = MainWindow.Instance.TaskbarItemInfo;
                taskbarItemInfo.ProgressValue = percent;
                if (percent > 0)
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                else
                    taskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;
            }
        });
    }

    public override async Task UpdateAsync(CancellationToken cancelToken)
    {
        // Update the current color based on the background frame and estimated time
        if (media == null || !HasMotion)
            return;

        double time = 0;
        if (runtime <= 0)
        {
            if (await TryUpdateTransportLayerTime(cancelToken))
            {
                runtime = 0;
                time = transportLayerTime;
            }
        }
        else if ((runtime % UpdateRate == 0) && await TryUpdateTransportLayerTime(cancelToken))
        {
            time = transportLayerTime;
        }
        else
        {
            time = transportLayerTime + (DateTime.Now - lastUpdateTime).TotalSeconds;
            if (time > media.Length)
                time %= media.Length;
        }

        lock (colorLock)
        {
            colors = media.GetData(pixelWidth, time);
        }

        runtime++;
    }

    public async Task<bool> TryUpdateTransportLayerTime(CancellationToken cancelToken)
    {
        double temp = await pro.AsyncGetTransportLayerTime(Layer.Presentation, cancelToken);
        if (double.IsNaN(temp))
            return false;
        transportLayerTime = temp;
        lastUpdateTime = DateTime.Now;
        return true;
    }

    public override ColorHSV GetColor(int fixtureId)
    {
        int index = idToIndex[fixtureId];
        ColorHSV result;
        lock(colorLock)
        {
            if (colors == null)
            {
                result = new ColorHSV(0, 0, 1); // White
            }
            else 
            {
                if (index >= colors.Length)
                    index = colors.Length - 1;
                result = (ColorHSV)colors[index];
            }
        }
        if (result.Saturation > saturation.Value)
            result.Saturation = saturation.Value;
        return result;
    }

    public override double GetIntensity(int fixtureid, ColorHSV color)
    {
        // intensity provided by user
        double targetMaxIntensity = intensity.GetIntensity(fixtureid);
        double targetMinIntensity = minIntensity.GetIntensity(fixtureid);
        if (targetMinIntensity >= targetMaxIntensity)
            return targetMaxIntensity;

        // intensity of the media 
        double maxChannelValue;
        double minChannelValue;
        lock (colorLock)
        {
            maxChannelValue = maxColorValue / 255d;
            minChannelValue = minColorValue / 255d;
        }

        double thisIntensity = color.Value;

        if(minChannelValue == maxChannelValue)
            return targetMaxIntensity;

        // https://math.stackexchange.com/a/914843
        // Input: [minChannelValue-maxChannelValue]
        // Output: [targetMinIntensity-targetMaxIntensity]
        double target = targetMinIntensity + (((targetMaxIntensity - targetMinIntensity) / (maxChannelValue - minChannelValue)) * (thisIntensity - minChannelValue));
        return target;
    }

}
