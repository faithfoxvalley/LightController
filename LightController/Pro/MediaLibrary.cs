using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task<ProMediaItem> LoadMediaAsync(int? id, string fileName, IProgress<double> progress, CancellationToken cancelToken)
        {
            if(motion)
                LogFile.Info("Starting motion generation for " + fileName);
            else
                LogFile.Info("Starting thumbnail generation for " + fileName);

            string fullPath = Path.Combine(mediaFolder, fileName);
            if(!File.Exists(fullPath))
            {
                fullPath = null;
                string mediaRoot = Path.GetDirectoryName(mediaFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                foreach(string folder in Directory.EnumerateDirectories(mediaRoot, "*", SearchOption.AllDirectories))
                {
                    string folderMedia = Path.Combine(folder, fileName);
                    if(File.Exists(folderMedia))
                    {
                        fullPath = folderMedia;
                        break;
                    }
                }
                if (fullPath == null)
                    throw new FileNotFoundException(fileName + " not found in " + mediaRoot, fileName);
            }
            

            ProMediaItem mediaItem = await ProMediaItem.GetItemAsync(
                cacheFolder,
                fullPath,
                motion,
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
