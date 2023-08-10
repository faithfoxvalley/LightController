using LightController.Pro.Packet;
using Pro.SerializationInterop.RVProtoData;
using ProPresenter.DO;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LightController.Pro
{
    public class ProPlaylistReader
    {
        private ProPresenter pro;

        public ProPlaylistReader(ProPresenter pro)
        {
            this.pro = pro;
        }

        public async Task Load()
        {
            PlaylistId playlistId = await GetCurrentPlaylist();
            if (!playlistId.playlist.HasValue)
                return;


            try
            {
                string playlistLibrary = Path.Combine(CommonPaths.Playlists, ConfigurationFilenames.LibraryPlaylist);
                if (ProSerialization.TryLoadFile(playlistLibrary, out PlaylistDocument playlists) && playlists.RootNode?.Playlists?.Playlists != null)
                {
                    Playlist currentPlaylist = playlists.RootNode.Playlists.Playlists.FirstOrDefault(x => x.Uuid?.String == playlistId.playlist.Value.uuid);
                }
            }
            catch (Exception e)
            {

            }
        }

        private async Task<PlaylistId> GetCurrentPlaylist()
        {
            PlaylistId activePlaylist = (await pro.GetActivePlaylistAsync()).presentation;
            if(activePlaylist.playlist.HasValue)
                return activePlaylist;
            return await pro.GetFocusedPlaylistAsync();
        }
    }
}
