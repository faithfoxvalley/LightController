using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightController.Pro
{
    public class MediaLibrary
    {
        private bool motion;
        private string cacheFolder;
        private string mediaFolder;
        private int mediaProcessors;
        private Dictionary<string, ProMediaItem> mediaNames = new Dictionary<string, ProMediaItem>();
        private Dictionary<int, ProMediaItem> mediaIds = new Dictionary<int, ProMediaItem>();

        public MediaLibrary(bool motion, string cacheFolder, string mediaFolder, int mediaProcessors)
        {
            this.motion = motion;
            this.cacheFolder = cacheFolder;
            this.mediaFolder = mediaFolder;
            this.mediaProcessors = mediaProcessors;
        }

        public bool TryGetExistingItem (int id, out ProMediaItem media)
        {
            return mediaIds.TryGetValue(id, out media);
        }

        public bool TryGetExistingItem (int? id, string fileName, out ProMediaItem media)
        {
            if (mediaNames.TryGetValue(fileName, out media))
            {
                if (id.HasValue)
                {
                    if (media.Id.HasValue && media.Id.Value != id.Value)
                        mediaIds.Remove(media.Id.Value);
                    mediaIds[id.Value] = media;
                    media.SetDetails(fileName, id, motion);
                }
                return true;
            }
            return false;
        }

        public async Task<ProMediaItem> LoadMediaAsync (int? id, string fileName, double duration, IProgress<double> progress, CancellationToken cancelToken)
        {
            if(motion)
                LogFile.Info("Starting motion generation for " + fileName);
            else
                LogFile.Info("Starting thumbnail generation for " + fileName);

            ProMediaItem mediaItem = await ProMediaItem.GetItemAsync(
                mediaFolder,
                cacheFolder,
                fileName,
                motion ? duration : 0,
                mediaProcessors, progress, cancelToken);
            mediaItem.SetDetails(fileName, id, motion);
            if (mediaItem.Id.HasValue)
                mediaIds[mediaItem.Id.Value] = mediaItem;
            mediaNames[mediaItem.Name] = mediaItem;

            if (motion)
                LogFile.Info("Finished motion generation for " + fileName);
            else
                LogFile.Info("Finished thumbnail generation for " + fileName);

            return mediaItem;
        }
    }
}
