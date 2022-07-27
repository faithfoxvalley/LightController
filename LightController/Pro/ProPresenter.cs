using System.Collections.Generic;
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
        private HttpClient client = new HttpClient();

        public List<ProLibrary> Libraries { get; } = new List<ProLibrary>();

        public ProPresenter(string url)
        {
            this.url = url;
        }

        public async Task AsyncInit()
        {
            ItemId[] libraries = await AsyncGetLibraries();
            if(libraries != null)
            {
                Libraries.Clear();
                Libraries.AddRange(libraries.Select(x => new ProLibrary(x)));
            }
        }

        public async Task AsyncUpdateLibraryData(ProLibrary library)
        {
            try
            {
                string responseBody = await client.GetStringAsync(url + "library/" + library.Uuid);
                LibraryItemList items = await Task.FromResult(JsonConvert.DeserializeObject<LibraryItemList>(responseBody));
                if(items.items != null)
                    library.UpdateLibraryData(items);
            }
            catch (HttpRequestException e)
            {
            }
        }





        public async Task<float> AsyncGetTransportLayerTime(Layer layer)
        {
            try
            {
                string responseBody = await client.GetStringAsync(url + layer.ToString().ToLowerInvariant() + "/time");
                if (float.TryParse(responseBody, out float time))
                    return time;
            }
            catch (HttpRequestException e)
            {
            }
            return float.NaN;
        }

        public async Task<TransportLayerStatus> AsyncGetTransportStatus(Layer layer)
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
