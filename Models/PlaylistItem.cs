using System.Collections.ObjectModel;
using System.Globalization;

namespace EasyPlay.Models
{
    public class PlaylistItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<VideoItem> Videos { get; set; } = new ObservableCollection<VideoItem>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastModifiedDate { get; set; }

        public string DisplayDate => CreatedDate.ToString("yyyy/MM/dd") ?? "";

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
                    "{0}/{1:00}/{2:00}",
                    pc.GetYear(date),
                    pc.GetMonth(date),
                    pc.GetDayOfMonth(date)
                );
            }
        }

        public int VideoCount => Videos.Count;
    }
}
