using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LightController.Pro.Packet;
using Newtonsoft.Json;

namespace LightController.Pro
{
    public class ProPresenter
    {
        private const string MotionCache = "Media";
        private const string ThumbnailCache = "Thumbnails";

        private string url;
        private string mediaPath;
        private HttpClient client = new HttpClient();
        private Dictionary<int, string> mediaNames = new Dictionary<int, string>();
        private Dictionary<string, ProMediaItem> media = new Dictionary<string, ProMediaItem>();
        private Dictionary<string, ProMediaItem> thumbnails = new Dictionary<string, ProMediaItem>();
        private System.Windows.Controls.ListBox mediaList;

        public ProPresenter(Config.ProPresenterConfig config, System.Windows.Controls.ListBox mediaList)
        {
            if(config?.ApiUrl == null)
            {
                ErrorBox.Show("No ProPresenter api url found, please check your config.");
                return;
            }
            url = config.ApiUrl;
            if(!url.EndsWith('/'))
                url += '/';

            if (config.MediaAssetsPath == null)
            {
                ErrorBox.Show("No ProPresenter media assets path found, please check your config.");
                return;
            }
            mediaPath = config.MediaAssetsPath;
            if (!mediaPath.EndsWith('/'))
                mediaPath += '/';

            this.mediaList = mediaList;
        }

        public async Task<ProMediaItem> GetCurrentMediaAsync (bool motion, CancellationToken cancelToken, int? id = null)
        {
            string mediaName;
            if(id.HasValue && mediaNames.TryGetValue(id.Value, out mediaName))
            {
                ProMediaItem existingItem;
                if (motion)
                {
                    if(media.TryGetValue(mediaName, out existingItem))
                        return existingItem;
                }
                else if (thumbnails.TryGetValue(mediaName, out existingItem))
                {
                    return existingItem;
                }
            }

            TransportLayerStatus status = await GetTransportStatusAsync(Layer.Presentation);
            if (status.audio_only || string.IsNullOrWhiteSpace(status.name))
                throw new HttpRequestException("No ProPresenter media available!");
            mediaName = status.name;
            if (id.HasValue)
                mediaNames[id.Value] = mediaName;

            ProMediaItem mediaItem;
            if (motion && status.duration > 0)
            {
                if (media.TryGetValue(mediaName, out ProMediaItem existingItem))
                    return existingItem;

                LogFile.Info("Starting media generation for " + mediaName);
                mediaItem = await ProMediaItem.GetItemAsync(
                    mediaPath,
                    Path.Combine(MainWindow.Instance.ApplicationData, MotionCache),
                    mediaName,
                    status.duration,
                    cancelToken);
                media[mediaName] = mediaItem;
                AddToMediaList(mediaName + " (motion)");
                LogFile.Info("Finished media generation for " + mediaName);
            }
            else
            {
                if (thumbnails.TryGetValue(mediaName, out ProMediaItem existingItem))
                    return existingItem;

                LogFile.Info("Starting thumbnail generation for " + mediaName);
                mediaItem = await ProMediaItem.GetItemAsync(
                    mediaPath,
                    Path.Combine(MainWindow.Instance.ApplicationData, ThumbnailCache),
                    mediaName,
                    0,
                    cancelToken);
                thumbnails[mediaName] = mediaItem;
                AddToMediaList(mediaName + " (thumbnail)");
                LogFile.Info("Finished thumbnail generation for " + mediaName);
            }

            return mediaItem;
        }



        public async Task<TransportLayerStatus> GetTransportStatusAsync(Layer layer)
        {
            try
            {
                string responseBody = await client.GetStringAsync(url + "transport/" + layer.ToString().ToLowerInvariant() + "/current");
                return await Task.FromResult(JsonConvert.DeserializeObject<TransportLayerStatus>(responseBody));
            }
            catch
            {
            }
            return await Task.FromResult(new TransportLayerStatus());
        }


        public async Task<double> AsyncGetTransportLayerTime(Layer layer)
        {
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                string responseBody = await client.GetStringAsync(url + "transport/" + layer.ToString().ToLowerInvariant() + "/time");
                double time = double.Parse(responseBody);
                sw.Stop();
                time += sw.ElapsedMilliseconds / 2000.0;
                return time;
            }
            catch
            {
                sw.Stop();
            }
            return double.NaN;
        }

        private void AddToMediaList(string mediaName)
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                mediaList.Items.Add(mediaName);
            });
        }
    }
}
