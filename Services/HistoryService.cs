using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using EasyPlay.Models;

namespace EasyPlay.Services
{
    public class HistoryService
    {
        private static readonly string HistoryFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EasyPlay",
            "history.json"
        );

        private ObservableCollection<VideoItem> _history = new ObservableCollection<VideoItem>();

        public HistoryService()
        {
            LoadHistory();
        }

        public ObservableCollection<VideoItem> GetHistory()
        {
            return _history;
        }

        public void AddToHistory(VideoItem video)
        {
            // Remove if already exists
            var existing = _history.FirstOrDefault(v => v.VideoUrl == video.VideoUrl);
            if (existing != null)
            {
                _history.Remove(existing);
            }

            // Update last played date
            video.LastPlayedDate = DateTime.Now;

            // Add to beginning
            _history.Insert(0, video);

            // Keep only last 100 items
            while (_history.Count > 100)
            {
                _history.RemoveAt(_history.Count - 1);
            }

            SaveHistory();
        }

        public void RemoveFromHistory(VideoItem video)
        {
            _history.Remove(video);
            SaveHistory();
        }

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        private void LoadHistory()
        {
            try
            {
                if (File.Exists(HistoryFilePath))
                {
                    var json = File.ReadAllText(HistoryFilePath);
                    var items = JsonSerializer.Deserialize<ObservableCollection<VideoItem>>(json);
                    if (items != null)
                    {
                        _history = items;
                    }
                }
            }
            catch
            {
                _history = new ObservableCollection<VideoItem>();
            }
        }

        private void SaveHistory()
        {
            try
            {
                var directory = Path.GetDirectoryName(HistoryFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(HistoryFilePath, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}
