using LightController.Color;
using MediaToolkit.Services;
using MediaToolkit.Tasks;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace LightController.Pro
{
    [ProtoContract(UseProtoMembersOnly = true)]
    public class ProMediaItem
    {
        private const string AppDataCache = "Media";
        private const double FrameInterval = 0.1;

        [ProtoMember(1)]
        private MediaFrame[] data;
        private Dictionary<int, MediaFrame[]> resizedData = new Dictionary<int, MediaFrame[]>();

        public ProMediaItem()
        {
        }

        public ColorRGB[] GetData(int size, double time)
        {
            MediaFrame[] frames;
            if (!resizedData.TryGetValue(size, out frames))
                frames = resizedData[size] = ResizeData(data, size);

            int index = (int)(time / FrameInterval);
            if (index >= data.Length)
                index = data.Length - 1;

            return frames[index].Data;
        }

        private MediaFrame[] ResizeData(MediaFrame[] data, int size)
        {
            MediaFrame[] newData = new MediaFrame[data.Length];
            for (int i = 0; i < data.Length; i++)
                newData[i] = data[i].ResizeData(size);
            return newData;
        }

        public static async Task<ProMediaItem> GetItemAsync(string mediaFolder, string file, double length)
        {
            string appdata = Path.Combine(MainWindow.Instance.ApplicationData, AppDataCache);
            Directory.CreateDirectory(appdata);
            if (!Directory.Exists(appdata))
                throw new Exception();
            string cacheFile = Path.Combine(appdata, Path.ChangeExtension(file, "bin"));
            if(File.Exists(cacheFile))
                return LoadItem(cacheFile);
            return await CreateItem(Path.Combine(mediaFolder, file), cacheFile, length);
        }

        private static async Task<ProMediaItem> CreateItem(string mediaPath, string cacheFile, double fileLength)
        {
            GetThumbnailOptions options = new GetThumbnailOptions
            {
                OutputFormat = OutputFormat.Image2,
                PixelFormat = MediaToolkit.Tasks.PixelFormat.Rgba,
                FrameSize = new FrameSize(854, 480)
            };

            List<MediaFrame> frames = new List<MediaFrame>();
            for (double time = 0; time < fileLength; time += FrameInterval)
            {
                options.SeekSpan = TimeSpan.FromSeconds(time);

                GetThumbnailResult thumbnailResult = await MainWindow.Instance.Ffmpeg.ExecuteAsync(new FfTaskGetThumbnail(
                  mediaPath,
                  options
                ));

                if (thumbnailResult.ThumbnailData.Length > 0)
                {
                    MediaFrame frame = await Task.Run(() => MediaFrame.CreateFrame(thumbnailResult.ThumbnailData, time));
                    frames.Add(frame);
                }
                else
                {
                    throw new Exception("Error while reading media file!");
                }
            }

            ProMediaItem result = new ProMediaItem();
            result.data = frames.ToArray();
            using (FileStream stream = File.Create(cacheFile))
            {
                Serializer.Serialize<ProMediaItem>(stream, result);
            }
            return result;
        }

        private static ProMediaItem LoadItem(string cacheFile)
        {
            using (FileStream stream = File.OpenRead(cacheFile))
            {
                return Serializer.Deserialize<ProMediaItem>(stream);
            }
        }
    }
}
