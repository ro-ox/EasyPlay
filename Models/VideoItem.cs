using System;

namespace EasyPlay.Models
{
    public class VideoItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? SubtitleUrl { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public DateTime? LastPlayedDate { get; set; }
        public long FileSize { get; set; }
        public string? LocalVideoPath { get; set; }
        public string? LocalSubtitlePath { get; set; }
        public bool IsDownloaded { get; set; }
        public TimeSpan? Duration { get; set; }
        public TimeSpan? LastPosition { get; set; }

        public string DisplayDate => AddedDate.ToString("yyyy/MM/dd HH:mm");
        public string DisplayLastPlayed => LastPlayedDate?.ToString("yyyy/MM/dd HH:mm") ?? "هرگز";
        public string DisplayDuration => Duration.HasValue
            ? $"{(int)Duration.Value.TotalHours:D2}:{Duration.Value.Minutes:D2}:{Duration.Value.Seconds:D2}"
            : "--:--:--";
    }
}
