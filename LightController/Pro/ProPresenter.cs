using System;
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
        private const double NetworkTimeout = 30;

        private string url;
        private HttpClient client = new HttpClient()
        {
            Timeout = TimeSpan.FromSeconds(NetworkTimeout)
        };
        private List<ProMediaItem> allMedia = new List<ProMediaItem>();
        private MediaLibrary motionLibrary;
        private MediaLibrary thumbnailLibrary;
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
            string mediaPath = config.MediaAssetsPath;
            if (!mediaPath.EndsWith(Path.DirectorySeparatorChar) && !mediaPath.EndsWith(Path.AltDirectorySeparatorChar))
                mediaPath += Path.DirectorySeparatorChar;
            int mediaProcessors = config.MaxMediaProcessors;
            if (mediaProcessors < 1)
                mediaProcessors = 1;
            motionLibrary = new MediaLibrary(true, Path.Combine(MainWindow.Instance.ApplicationData, MotionCache), mediaPath, mediaProcessors);
            thumbnailLibrary = new MediaLibrary(false, Path.Combine(MainWindow.Instance.ApplicationData, ThumbnailCache), mediaPath, mediaProcessors);

            this.mediaList = mediaList;
        }

        public async Task<ProMediaItem> GetCurrentMediaAsync (bool motion, IProgress<double> progress, CancellationToken cancelToken, int? id = null)
        {
            string mediaName;
            if (id.HasValue)
            {
                ProMediaItem existingItem;
                if (motion)
                {
                    if (motionLibrary.TryGetExistingItem(id.Value, out existingItem))
                    {
                        await UpdateMediaList(existingItem);
                        return existingItem;
                    }
                }
                else if (thumbnailLibrary.TryGetExistingItem(id.Value, out existingItem))
                {
                    await UpdateMediaList(existingItem);
                    return existingItem;
                }
            }

            TransportLayerStatus status = await GetTransportStatusAsync(Layer.Presentation);
            if (status.audio_only || string.IsNullOrWhiteSpace(status.name))
                throw new HttpRequestException("No ProPresenter media available!");
            mediaName = status.name;

            MediaLibrary library;
            if (motion)
                library = motionLibrary;
            else
                library = thumbnailLibrary;

            ProMediaItem mediaItem;
            if (!library.TryGetExistingItem(id, status.name, out mediaItem))
            {
                mediaItem = await library.LoadMediaAsync(id, mediaName, status.duration, progress, cancelToken);
                allMedia.Add(mediaItem);
            }

            allMedia.Sort(new MediaItemComparer());
            await UpdateMediaList(mediaItem);
            return mediaItem;
        }


        public async Task DeselectMediaItem()
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                mediaList.SelectedIndex = -1;
            });
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

        private async Task UpdateMediaList(ProMediaItem currentItem)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var list = mediaList.Items;
                list.Clear();
                
                int currentIndex = -1;
                int i = 0;
                foreach(var media in allMedia)
                {
                    if (ReferenceEquals(currentItem, media))
                        currentIndex = i;
                    list.Add(media.ToString());
                    i++;
                }

                if(currentIndex >= 0)
                    mediaList.SelectedIndex = currentIndex;
            });
        }
    }
}
