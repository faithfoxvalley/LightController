using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using LightController.Color;
using ProtoBuf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightController.Pro;

[ProtoContract(UseProtoMembersOnly = true)]
public class ProMediaItem
{
    private const double FrameInterval = 0.1;
    private static readonly TimeSpan FrameIntervalSpan = TimeSpan.FromSeconds(FrameInterval);
    private static readonly Size FrameSize = new Size(854, 480);

    [ProtoMember(1)]
    private MediaFrame[] data;
    private Dictionary<int, MediaFrame[]> resizedData = new Dictionary<int, MediaFrame[]>();
    private string details = "";

    public double Length => data.Length * FrameInterval;
    public string Name { get; private set; }
    public int? Id { get; private set; }
    public bool HasMotion { get; private set; }

    public ProMediaItem()
    {
    }

    public void SetDetails(string name, int? id, bool hasMotion)
    {
        Name = name;
        Id = id;
        HasMotion = hasMotion;

        StringBuilder sb = new StringBuilder();
        if (Id.HasValue)
            sb.Append(Id.Value).Append(" - ");
        sb.Append(Name);
        if (!HasMotion)
            sb.Append(" (thumbnail)");
        details = sb.ToString();
    }

    /// <summary>
    /// Get maximum and minimum HSV value of all the colors in the media
    /// </summary>
    public void GetColorValueBounds(int size, out byte max, out byte min)
    {
        MediaFrame[] frames;
        if (!resizedData.TryGetValue(size, out frames))
            frames = resizedData[size] = ResizeData(data, size);

        min = byte.MaxValue;
        max = byte.MinValue;
        foreach(MediaFrame frame in frames)
        {
            foreach(ColorRGB color in frame.Data)
            {
                byte value = color.Max();
                if (value < min)
                    min = value;
                if (value > max)
                    max = value;
            }
        }

    }

    public ColorRGB[] GetData(int size, double time)
    {
        if (time < 0)
            time = 0;

        MediaFrame[] frames;
        if (!resizedData.TryGetValue(size, out frames))
            frames = resizedData[size] = ResizeData(data, size);

        if (frames.Length == 1)
            return frames[0].Data;

        int index = (int)(time / FrameInterval) % data.Length;

        double indexRemainder = (time % FrameInterval) / FrameInterval;
        if(indexRemainder < 0.000001)
            return frames[index].Data;

        int nextIndex = (index + 1) % data.Length;
        return frames[index].Interpolate(frames[nextIndex], indexRemainder);
    }

    private MediaFrame[] ResizeData(MediaFrame[] data, int size)
    {
        MediaFrame[] newData = new MediaFrame[data.Length];
        for (int i = 0; i < data.Length; i++)
            newData[i] = data[i].ResizeData(size);
        return newData;
    }

    public static Task<ProMediaItem> GetItemAsync(string cacheFolder, string file, bool motion, int mediaProcessors, IProgress<double> progress, CancellationToken cancelToken)
    {
        Directory.CreateDirectory(cacheFolder);
        string cacheFile = Path.Combine(cacheFolder, Path.GetFileName(file) + ".bin");
#if !DEBUG
        if(File.Exists(cacheFile))
        {
            progress.Report(double.NaN);
            return LoadItemAsync(cacheFile, cancelToken);
        }
#endif
        return CreateItemAsync(file, cacheFile, motion, mediaProcessors, progress, cancelToken);
    }

    private static async Task<ProMediaItem> CreateItemAsync(string mediaPath, string cacheFile, bool motion, int mediaProcessors, IProgress<double> progress, CancellationToken cancelToken)
    {
        progress.Report(double.NaN);

        ConcurrentBag<MediaFrame> frames = new ConcurrentBag<MediaFrame>();

        TimeSpan fileLength = TimeSpan.Zero;
        if(motion)
        {
            IMediaAnalysis source = await FFProbe.AnalyseAsync(mediaPath, cancellationToken: cancelToken);
            if(source?.VideoStreams != null && source.VideoStreams.Count > 0)
                fileLength = source.VideoStreams[0].Duration;
        }

        if(fileLength == TimeSpan.Zero)
        {
            MediaFrame frame = await CreateFrameAsync(mediaPath, FrameSize, TimeSpan.Zero, cancelToken);
            if (frame != null)
                frames.Add(frame);
        }
        else
        {
            List<TimeSpan> tasks = new List<TimeSpan>();
            for (TimeSpan time = TimeSpan.Zero; time < fileLength; time += FrameIntervalSpan)
                tasks.Add(time);

            ParallelOptions parallelOptions = new ParallelOptions()
            {
                CancellationToken = cancelToken,
                MaxDegreeOfParallelism = mediaProcessors,
            };

            await Parallel.ForEachAsync(tasks, parallelOptions, async (x, c) =>
            {
                MediaFrame frame = await CreateFrameAsync(mediaPath, FrameSize, x, cancelToken);
                if (frame != null)
                {
                    frames.Add(frame);
                    progress.Report(frames.Count / (double)tasks.Count);
                }
            });
        }

        cancelToken.ThrowIfCancellationRequested();

        progress.Report(double.NaN);

        if (frames.Count == 0)
            throw new Exception("Error while reading media file: Unable to get any frames from the media.");

        ProMediaItem result = new ProMediaItem();
        result.data = frames.OrderBy(x => x.Time).ToArray();
        using (FileStream stream = File.Create(cacheFile))
        {
            await Task.Run(() => Serializer.Serialize<ProMediaItem>(stream, result));
        }

        return result;
    }

    private static async Task<MediaFrame> CreateFrameAsync(string input, Size size, TimeSpan time, CancellationToken cancelToken)
    {
        using MemoryStream ms = new MemoryStream();

        await FFMpegArguments
            .FromFileInput(input, false, options =>
            {
                if (time > TimeSpan.Zero)
                    options.Seek(time);
            })
            .OutputToPipe(new StreamPipeSink(ms), options => options
                .SelectStream(0)
                .WithVideoCodec(VideoCodec.Image.Png)
                .WithFrameOutputCount(1)
                .Resize(size)
                .ForcePixelFormat("rgb24")
                .ForceFormat("rawvideo"))
            .CancellableThrough(cancelToken)
            .ProcessAsynchronously(true);

        ms.Position = 0;

        cancelToken.ThrowIfCancellationRequested();
        if (ms.Length == 0)
        {
            Log.Warn($"Failed to get color information from '{input}' at time {time:g}");
            return null;
        }

        return await Task.Run(() => MediaFrame.CreateFrame(ms, time, cancelToken));
    }

    private static async Task<ProMediaItem> LoadItemAsync(string cacheFile, CancellationToken cancelToken)
    {
        using (FileStream stream = File.OpenRead(cacheFile))
        {
            cancelToken.ThrowIfCancellationRequested();
            return await Task.Run(() => Serializer.Deserialize<ProMediaItem>(stream));
        }
    }

    public override string ToString()
    {
        return details;
    }
}
