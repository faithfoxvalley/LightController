using LightController.Color;
using MediaToolkit.Tasks;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace LightController.Pro
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class ProMediaItem
    {
        private const double FrameInterval = 0.1;

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

        public static Task<ProMediaItem> GetItemAsync(string mediaFolder, string cacheFolder, string file, double length, int mediaProcessors, IProgress<double> progress, CancellationToken cancelToken)
        {
            Directory.CreateDirectory(cacheFolder);
            string cacheFile = Path.Combine(cacheFolder, file + ".bin");
            if(File.Exists(cacheFile))
            {
                progress.Report(double.NaN);
                return LoadItemAsync(cacheFile, cancelToken);
            }
            return CreateItemAsync(Path.Combine(mediaFolder, file), cacheFile, length, mediaProcessors, progress, cancelToken);
        }

        private static async Task<ProMediaItem> CreateItemAsync(string mediaPath, string cacheFile, double fileLength, int mediaProcessors, IProgress<double> progress, CancellationToken cancelToken)
        {
            progress.Report(double.NaN);

            GetThumbnailOptions options = new GetThumbnailOptions
            {
                OutputFormat = OutputFormat.Image2,
                PixelFormat = PixelFormat.Rgba,
                FrameSize = new FrameSize(854, 480)
            };

            ConcurrentBag<MediaFrame> frames = new ConcurrentBag<MediaFrame>();

            if(fileLength == 0)
            {
                MediaFrame frame = await CreateFrameAsync(mediaPath, true, options, 0, cancelToken);
                if (frame != null)
                    frames.Add(frame);
            }
            else
            {
                List<double> tasks = new List<double>((int)(fileLength / FrameInterval) + 1);
                for (double time = 0; time < fileLength; time += FrameInterval)
                    tasks.Add(time);

                ParallelOptions parallelOptions = new ParallelOptions()
                {
                    CancellationToken = cancelToken,
                    MaxDegreeOfParallelism = mediaProcessors,
                };

                await Parallel.ForEachAsync(tasks, parallelOptions, async (x, c) =>
                {
                    MediaFrame frame = await CreateFrameAsync(mediaPath, false, options, x, cancelToken);
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

        private static async Task<MediaFrame> CreateFrameAsync(string mediaPath, bool isImage, GetThumbnailOptions options, double time, CancellationToken cancelToken)
        {
            options.SeekSpan = TimeSpan.FromSeconds(time);

            GetThumbnailResult thumbnailResult = await MainWindow.Instance.Ffmpeg.ExecuteAsync(new FfTaskGetThumbnail(
              mediaPath,
              options
            ));

            cancelToken.ThrowIfCancellationRequested();

            if (thumbnailResult.ThumbnailData.Length > 0)
            {
                return await Task.Run(() => MediaFrame.CreateFrame(thumbnailResult.ThumbnailData, time, cancelToken));
            }
            else
            {
                // Try again with slightly modified ffmpeg arguments if the media is an image
                if (isImage)
                {
                    thumbnailResult = await MainWindow.Instance.Ffmpeg.ExecuteAsync(new FfTaskGetThumbnail2(
                      mediaPath,
                      options
                    ));

                    cancelToken.ThrowIfCancellationRequested();

                    if (thumbnailResult.ThumbnailData.Length > 0)
                        return await Task.Run(() => MediaFrame.CreateFrame(thumbnailResult.ThumbnailData, 0, cancelToken));
                    else
                        return null;
                }
                else
                {
                    return null;
                }
            }

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
}
