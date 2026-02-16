using System;
using System.Globalization;

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

        public string DisplayDate => AddedDate.ToString("yyyy/MM/dd HH:mm") ?? "";

        public string DisplayDateShamsi
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DisplayDate))
                    return "";

                if (!DateTime.TryParse(DisplayDate, out var date))
                    return DisplayDate!;

                var pc = new PersianCalendar();

                return string.Format(
                    "{0}/{1:00}/{2:00} {3:00}:{4:00}",
                    pc.GetYear(date),
                    pc.GetMonth(date),
                    pc.GetDayOfMonth(date),
                    pc.GetHour(date),
                    pc.GetMinute(date)
                );
            }
        }

        public string DisplayLastPlayed => LastPlayedDate?.ToString("yyyy/MM/dd HH:mm") ?? "هرگز";
        public string DisplayLastPlayedShamsi
        {
            get
            {
                if (!LastPlayedDate.HasValue)
                    return "هرگز";

                var pc = new PersianCalendar();
                var date = LastPlayedDate.Value;

                return string.Format(
                    "{0}/{1:00}/{2:00} {3:00}:{4:00}",
                    pc.GetYear(date),
                    pc.GetMonth(date),
                    pc.GetDayOfMonth(date),
                    pc.GetHour(date),
                    pc.GetMinute(date)
                );
            }
        }

        public string DisplayDuration => Duration.HasValue
            ? $"{(int)Duration.Value.TotalHours:D2}:{Duration.Value.Minutes:D2}:{Duration.Value.Seconds:D2}"
            : "--:--:--";
    }
}
