using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasyPlay.Helpers;
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

            Title = $"پلی لیست: {playlist.Name}";
            PlaylistNameText.Text = playlist.Name;
            VideosListBox.ItemsSource = playlist.Videos;
        }

        #region Custom Title Bar

        // Drag
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                // Ignore
            }
            else
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    var mousePosition = PointToScreen(e.GetPosition(this));

                    var percentHorizontal = e.GetPosition(this).X / this.ActualWidth;

                    this.WindowState = WindowState.Normal;

                    this.Left = mousePosition.X - (this.Width * percentHorizontal);
                    this.Top = mousePosition.Y - e.GetPosition(this).Y;

                    this.Left = Math.Max(0, Math.Min(this.Left,
                        SystemParameters.VirtualScreenWidth - this.Width));
                    this.Top = Math.Max(0, Math.Min(this.Top,
                        SystemParameters.VirtualScreenHeight - this.Height));
                }

                try
                {
                    this.DragMove();
                }
                catch
                {
                    // Ignore
                }
            }
        }

        // Close
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // State
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
        }

        #endregion

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
                var result = CustomMessageBox.ShowQuestion(
                    $"آیا مطمئن هستید که می خواهید '{video.Title}' را از پلی لیست حذف کنید؟",
                    "تأیید حذف");

                if (result == CustomMessageBox.MessageResult.Yes)
                {
                    _playlistService.RemoveVideoFromPlaylist(_playlist, video);
                    VideosListBox.ItemsSource = null;
                    VideosListBox.ItemsSource = _playlist.Videos;
                }
            }
        }
    }
}
