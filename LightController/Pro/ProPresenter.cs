using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LightController.Pro.Packet;
using Newtonsoft.Json;

namespace LightController.Pro
{
    public class ProPresenter
    {
        private string url;
        private string mediaPath;
        private HttpClient client = new HttpClient();
        private Dictionary<string, ProMediaItem> media = new Dictionary<string, ProMediaItem>();

        public ProPresenter(Config.ProPresenterConfig config)
        {
            url = config.ApiUrl;
            if(!url.EndsWith('/'))
                url += '/';

            mediaPath = config.MediaAssetsPath;
            if (!mediaPath.EndsWith('/'))
                mediaPath += '/';
        }

        public async Task<ProMediaItem> GetCurrentMediaAsync ()
        {
            TransportLayerStatus status = await GetTransportStatusAsync(Layer.Presentation);
            if(!status.audio_only && status.duration > 0 && !string.IsNullOrWhiteSpace(status.name))
            {
                if(media.TryGetValue(status.name, out ProMediaItem existingItem))
                    return existingItem;

                return await ProMediaItem.GetItemAsync(mediaPath, status.name, status.duration);
            }
            else
            {
                throw new Exception("No media available!");
            }
        }    


        public async Task<TransportLayerStatus> GetTransportStatusAsync(Layer layer)
        {
            try
            {
                string responseBody = await client.GetStringAsync(url + "transport/" + layer.ToString().ToLowerInvariant() + "/current");
                return await Task.FromResult(JsonConvert.DeserializeObject<TransportLayerStatus>(responseBody));
            }
            catch (HttpRequestException e)
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

        public async Task<byte[]> AsyncGetThumbnail(string uuid, int width)
        {

            try
            {
                return await client.GetByteArrayAsync(url + uuid + "/thumbnail");
            }
            catch (HttpRequestException e)
            {
            }
            return new byte[0];
        }

        public async Task<ItemId[]> AsyncGetLibraries()
        {
            try
            {
                string responseBody = await client.GetStringAsync(url + "libraries");
                return await Task.FromResult(JsonConvert.DeserializeObject<ItemId[]>(responseBody));
            }
            catch (HttpRequestException e)
            {
            }
            return await Task.FromResult(new ItemId[0]);
        }

        public async Task<Presentation> AsyncGetCurrentPresentation()
        {
            try
            {
                string responseBody = await client.GetStringAsync(url + "presentation/active");
                return await Task.FromResult(JsonConvert.DeserializeObject<Presentation>(responseBody));
            }
            catch (HttpRequestException e)
            {
            }
            return await Task.FromResult(new Presentation());
        }
    }
}
