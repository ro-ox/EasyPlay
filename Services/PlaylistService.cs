using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using EasyPlay.Models;

namespace EasyPlay.Services
{
    public class PlaylistService
    {
        private static readonly string PlaylistFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasyPlay",
            "playlists.json"
        );

        private ObservableCollection<PlaylistItem> _playlists = new ObservableCollection<PlaylistItem>();

        public PlaylistService()
        {
            LoadPlaylists();
        }

        public ObservableCollection<PlaylistItem> GetPlaylists()
        {
            return _playlists;
        }

        public PlaylistItem CreatePlaylist(string name)
        {
            var playlist = new PlaylistItem { Name = name };
            _playlists.Add(playlist);
            SavePlaylists();
            return playlist;
        }

        public void AddVideoToPlaylist(PlaylistItem playlist, VideoItem video)
        {
            // Check if video already exists
            if (!playlist.Videos.Any(v => v.VideoUrl == video.VideoUrl))
            {
                playlist.Videos.Add(video);
                playlist.LastModifiedDate = DateTime.Now;
                SavePlaylists();
            }
        }

        public void RemoveVideoFromPlaylist(PlaylistItem playlist, VideoItem video)
        {
            playlist.Videos.Remove(video);
            playlist.LastModifiedDate = DateTime.Now;
            SavePlaylists();
        }

        public void DeletePlaylist(PlaylistItem playlist)
        {
            _playlists.Remove(playlist);
            SavePlaylists();
        }

        public void RenamePlaylist(PlaylistItem playlist, string newName)
        {
            playlist.Name = newName;
            playlist.LastModifiedDate = DateTime.Now;
            SavePlaylists();
        }

        private void LoadPlaylists()
        {
            try
            {
                if (File.Exists(PlaylistFilePath))
                {
                    var json = File.ReadAllText(PlaylistFilePath);
                    var items = JsonSerializer.Deserialize<ObservableCollection<PlaylistItem>>(json);
                    if (items != null)
                    {
                        _playlists = items;
                    }
                }
            }
            catch
            {
                _playlists = new ObservableCollection<PlaylistItem>();
            }
        }

        private void SavePlaylists()
        {
            try
            {
                var directory = Path.GetDirectoryName(PlaylistFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_playlists, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(PlaylistFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
