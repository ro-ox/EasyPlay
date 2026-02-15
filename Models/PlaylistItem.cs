using System.Collections.ObjectModel;

namespace EasyPlay.Models
{
    public class PlaylistItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public ObservableCollection<VideoItem> Videos { get; set; } = new ObservableCollection<VideoItem>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastModifiedDate { get; set; }

        public string DisplayDate => CreatedDate.ToString("yyyy/MM/dd");
        public int VideoCount => Videos.Count;
    }
}
