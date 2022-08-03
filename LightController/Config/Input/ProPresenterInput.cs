using LightController.Color;
using LightController.Pro;
using MediaToolkit.Tasks;
using System.IO;
using System.Threading.Tasks;

namespace LightController.Config.Input
{
    // TODO
    [YamlTag("!propresenter_input")]
    public class ProPresenterInput : InputBase
    {
        private ProPresenter pro = null;
        private ProMediaItem media;
        private int min;
        private int max;

        public ProPresenterInput() { }

        public override ColorRGB GetColor()
        {
            return new ColorRGB(1, 1, 0);
        }

        public override void Init()
        {
            //pro = new ProPresenter("http://localhost:1025/v1/");
            min = FixtureIds.Min();
            max = FixtureIds.Max();
        }


        public override async void Start()
        {
            // TODO: Initialize info about current background
            media = null;
            media = await pro.GetCurrentMediaAsync();

            /*var status = await pro.AsyncGetTransportStatus(Layer.Presentation);
            if (status.is_playing && Path.HasExtension(status.name))
            {
                string path = Path.Combine(pro.MediaAssetsPath, status.name);
                if (File.Exists(path))
                {
                    GetThumbnailOptions options = new GetThumbnailOptions
                    {
                        SeekSpan = TimeSpan.FromSeconds(10),
                        OutputFormat = OutputFormat.Image2,
                        PixelFormat = MediaToolkit.Tasks.PixelFormat.Rgba
                    };

                    GetThumbnailResult result = await service.ExecuteAsync(new FfTaskGetThumbnail(
                      path,
                      options
                    ));

                    string newFile = @"C:\Users\austin.vaness\Desktop\Test\image.jpg";

                    BitmapProcessing.ReadImage(result.ThumbnailData, 14, 0.1);
                }

            }*/
        }

        public override void Update()
        {
            // TODO: Update the current color based on the background frame and estimated time
            if (media == null)
                return;


        }
    }
}
