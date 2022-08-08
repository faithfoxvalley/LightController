using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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
        private Dictionary<string, ProMediaItem> media = new Dictionary<string, ProMediaItem>();
        private Dictionary<string, ProMediaItem> thumbnails = new Dictionary<string, ProMediaItem>();

        public ProPresenter(Config.ProPresenterConfig config)
        {
            url = config.ApiUrl;
            if(!url.EndsWith('/'))
                url += '/';

            mediaPath = config.MediaAssetsPath;
            if (!mediaPath.EndsWith('/'))
                mediaPath += '/';
        }

        public async Task<ProMediaItem> GetCurrentMediaAsync (bool motion, CancellationToken cancelToken)
        {
            TransportLayerStatus status = await GetTransportStatusAsync(Layer.Presentation);
            if(!status.audio_only && !string.IsNullOrWhiteSpace(status.name))
            {
                ProMediaItem mediaItem;
                if (motion && status.duration > 0)
                {
                    if (media.TryGetValue(status.name, out ProMediaItem existingItem))
                        return existingItem;

                    LogFile.Info("Starting media generation for " + status.name);
                    mediaItem = await ProMediaItem.GetItemAsync(
                        mediaPath,
                        Path.Combine(MainWindow.Instance.ApplicationData, MotionCache),
                        status.name,
                        status.duration,
                        cancelToken);
                    media[status.name] = mediaItem;
                    LogFile.Info("Finished media generation for " + status.name);
                }
                else
                {
                    if (thumbnails.TryGetValue(status.name, out ProMediaItem existingItem))
                        return existingItem;

                    LogFile.Info("Starting thumbnail generation for " + status.name);
                    mediaItem = await ProMediaItem.GetItemAsync(
                        mediaPath,
                        Path.Combine(MainWindow.Instance.ApplicationData, ThumbnailCache),
                        status.name,
                        0,
                        cancelToken);
                    thumbnails[status.name] = mediaItem;
                    LogFile.Info("Finished thumbnail generation for " + status.name);
                }

                
                return mediaItem;
            }
            else
            {
                throw new HttpRequestException("No ProPresenter media available!");
            }
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
    }
}
