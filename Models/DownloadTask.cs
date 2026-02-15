using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasyPlay.Models
{
    public class DownloadTask : INotifyPropertyChanged
    {
        private string _status = "در حال دانلود...";
        private double _progress;
        private long _downloadedBytes;
        private long _totalBytes;
        private double _downloadSpeed;
        private bool _isCompleted;
        private bool _isCancelled;
        private string _errorMessage = string.Empty;

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string SavePath { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        public double Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
        }

        public long DownloadedBytes
        {
            get => _downloadedBytes;
            set
            {
                _downloadedBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DownloadedText));
            }
        }

        public long TotalBytes
        {
            get => _totalBytes;
            set
            {
                _totalBytes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(DownloadedText));
            }
        }

        public double DownloadSpeed
        {
            get => _downloadSpeed;
            set
            {
                _downloadSpeed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SpeedText));
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set { _isCompleted = value; OnPropertyChanged(); }
        }

        public bool IsCancelled
        {
            get => _isCancelled;
            set { _isCancelled = value; OnPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public string ProgressText => $"{Progress:F1}%";
        public string SpeedText => FormatBytes(DownloadSpeed) + "/s";
        public string DownloadedText => $"{FormatBytes(DownloadedBytes)} / {FormatBytes(TotalBytes)}";
        public string TotalText => FormatBytes(TotalBytes);

        private string FormatBytes(double bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }
            return $"{bytes:0.##} {sizes[order]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
