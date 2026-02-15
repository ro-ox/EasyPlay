using System.Windows;
using System.Windows.Controls;
using EasyPlay.Models;
using EasyPlay.Services;

namespace EasyPlay.Views
{
    public partial class PlaylistViewDialog : Window
    {
        private readonly PlaylistItem _playlist;
        private readonly PlaylistService _playlistService;

        public PlaylistViewDialog(PlaylistItem playlist, PlaylistService playlistService)
        {
            InitializeComponent();
            _playlist = playlist;
            _playlistService = playlistService;

            Title = $"پلی‌لیست: {playlist.Name}";
            PlaylistNameText.Text = playlist.Name;
            VideosListBox.ItemsSource = playlist.Videos;
        }

        private void PlayVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is VideoItem video)
            {
                var playerWindow = new VideoPlayerWindow(video.VideoUrl, video.Title, video.SubtitleUrl);
                playerWindow.Show();
            }
        }

        private void RemoveVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is VideoItem video)
            {
                var result = MessageBox.Show(
                    $"آیا مطمئن هستید که می‌خواهید '{video.Title}' را از پلی‌لیست حذف کنید؟",
                    "تأیید حذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    _playlistService.RemoveVideoFromPlaylist(_playlist, video);
                    VideosListBox.ItemsSource = null;
                    VideosListBox.ItemsSource = _playlist.Videos;
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
