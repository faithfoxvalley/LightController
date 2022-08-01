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
        const string Url = "http://localhost:1025/v1/";

        public List<ProLibrary> Libraries { get; } = new List<ProLibrary>();

        private HttpClient client = new HttpClient();

        public ProPresenter() { }

        public ProPresenter(Config.ProPresenterConfig config)
        {
            AsyncInit();
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
                string responseBody = await client.GetStringAsync(Url + "library/" + library.Uuid);
                LibraryItemList items = await Task.FromResult(JsonConvert.DeserializeObject<LibraryItemList>(responseBody));
                if(items.items != null)
                    library.UpdateLibraryData(items);
            }
            catch (HttpRequestException e)
            {
            }
        }




        public async Task<TransportLayerStatus> AsyncGetTransportStatus(Layer layer)
        {
            try
            {
                string responseBody = await client.GetStringAsync(Url + "transport/" + layer.ToString().ToLowerInvariant() + "/current");
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
                string responseBody = await client.GetStringAsync(Url + "transport/" + layer.ToString().ToLowerInvariant() + "/time");
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
                return await client.GetByteArrayAsync(Url + uuid + "/thumbnail");
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
                string responseBody = await client.GetStringAsync(Url + "libraries");
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
                string responseBody = await client.GetStringAsync(Url + "presentation/active");
                return await Task.FromResult(JsonConvert.DeserializeObject<Presentation>(responseBody));
            }
            catch (HttpRequestException e)
            {
            }
            return await Task.FromResult(new Presentation());
        }
    }
}
