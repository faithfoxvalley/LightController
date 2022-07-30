using LightController.Pro;
using System.IO;
using System.Threading.Tasks;

namespace LightController.Config.Input
{
    [YamlTag("!propresenter_input")]
    public class ProPresenterInput : InputBase
    {
        private ProPresenter pro;

        public ProPresenterInput() { }

        public override Task Init()
        {
            pro = new ProPresenter("http://localhost:1025/v1/");

        }


        public override async Task Start()
        {
            var status = await pro.AsyncGetTransportStatus(Layer.Presentation);
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

            }
        }
    }
}
