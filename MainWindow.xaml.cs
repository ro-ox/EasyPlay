using System.IO;
using System.Windows;
using System.Windows.Controls;
using EasyPlay.Helpers;
using EasyPlay.Models;
using EasyPlay.Services;
using EasyPlay.Views;

namespace EasyPlay
{
    public partial class MainWindow : Window
    {
        private readonly HistoryService _historyService;
        private readonly PlaylistService _playlistService;
        private readonly DownloadService _downloadService;
        private CancellationTokenSource? _downloadCancellationTokenSource;
        private DownloadTask? _currentDownload;

        public MainWindow()
        {
            InitializeComponent();

            _historyService = new HistoryService();
            _playlistService = new PlaylistService();
            _downloadService = new DownloadService();

            LoadData();

            // Set default save path
            SavePathTextBox.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "EasyPlay"
            );
        }

        private void LoadData()
        {
            // Load playlists
            PlaylistsListBox.ItemsSource = _playlistService.GetPlaylists();

            // Load history
            HistoryListBox.ItemsSource = _historyService.GetHistory();
        }

        #region Play Tab Events

        private void DownloadVideoCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DownloadVideoCheckBox.IsChecked == true)
            {
                DownloadOptionsPanel.IsEnabled = true;
                DownloadOptionsPanel.Opacity = 1.0;
            }
            else
            {
                DownloadOptionsPanel.IsEnabled = false;
                DownloadOptionsPanel.Opacity = 0.5;
            }
        }

        private void BrowseSavePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "انتخاب پوشه ذخیره",
                FileName = "انتخاب این پوشه",
                Filter = "فولدر|*.folder",
                CheckFileExists = false,
                CheckPathExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                var path = System.IO.Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(path))
                {
                    SavePathTextBox.Text = path;
                }
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var videoUrl = VideoUrlTextBox.Text.Trim();
            var title = VideoTitleTextBox.Text.Trim();
            var subtitleUrl = SubtitleUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(videoUrl))
            {
                MessageBox.Show("لطفاً لینک ویدیو را وارد کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(title))
            {
                title = "ویدیوی بدون عنوان";
            }

            // Validate URLs
            if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out _))
            {
                MessageBox.Show("لینک ویدیو معتبر نیست.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(subtitleUrl) && !Uri.TryCreate(subtitleUrl, UriKind.Absolute, out _))
            {
                MessageBox.Show("لینک زیرنویس معتبر نیست.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create video item
            var videoItem = new VideoItem
            {
                Title = title,
                VideoUrl = videoUrl,
                SubtitleUrl = string.IsNullOrEmpty(subtitleUrl) ? null : subtitleUrl
            };

            // Add to history
            _historyService.AddToHistory(videoItem);
            HistoryListBox.ItemsSource = _historyService.GetHistory();

            // Start download if checked
            if (DownloadVideoCheckBox.IsChecked == true)
            {
                var savePath = SavePathTextBox.Text.Trim();
                if (string.IsNullOrEmpty(savePath))
                {
                    MessageBox.Show("لطفاً مسیر ذخیره را انتخاب کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await StartDownloadAsync(videoUrl, savePath, title, subtitleUrl);
            }

            // Open player
            var playerWindow = new VideoPlayerWindow(
                videoUrl,
                title,
                string.IsNullOrEmpty(subtitleUrl) ? null : subtitleUrl
            );
            playerWindow.Show();
        }

        private void AddToPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var videoUrl = VideoUrlTextBox.Text.Trim();
            var title = VideoTitleTextBox.Text.Trim();
            var subtitleUrl = SubtitleUrlTextBox.Text.Trim();

            if (string.IsNullOrEmpty(videoUrl))
            {
                MessageBox.Show("لطفاً لینک ویدیو را وارد کنید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(title))
            {
                title = "ویدیوی بدون عنوان";
            }

            // Select or create playlist
            var playlists = _playlistService.GetPlaylists();
            if (playlists.Count == 0)
            {
                var result = MessageBox.Show(
                    "هیچ پلی‌لیستی وجود ندارد. آیا می‌خواهید یک پلی‌لیست جدید ایجاد کنید؟",
                    "پلی‌لیست",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    var playlistName = Microsoft.VisualBasic.Interaction.InputBox(
                        "نام پلی‌لیست جدید:",
                        "ایجاد پلی‌لیست",
                        "پلی‌لیست من"
                    );

                    if (!string.IsNullOrEmpty(playlistName))
                    {
                        var newPlaylist = _playlistService.CreatePlaylist(playlistName);
                        AddVideoToPlaylist(newPlaylist, videoUrl, title, subtitleUrl);
                    }
                }
            }
            else
            {
                // Show playlist selection dialog
                var dialog = new PlaylistSelectionDialog(playlists.ToList());
                if (dialog.ShowDialog() == true && dialog.SelectedPlaylist != null)
                {
                    AddVideoToPlaylist(dialog.SelectedPlaylist, videoUrl, title, subtitleUrl);
                }
            }
        }

        private void AddVideoToPlaylist(PlaylistItem playlist, string videoUrl, string title, string? subtitleUrl)
        {
            var videoItem = new VideoItem
            {
                Title = title,
                VideoUrl = videoUrl,
                SubtitleUrl = subtitleUrl
            };

            _playlistService.AddVideoToPlaylist(playlist, videoItem);
            PlaylistsListBox.ItemsSource = _playlistService.GetPlaylists();

            MessageBox.Show($"ویدیو به پلی‌لیست '{playlist.Name}' اضافه شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Playlist Tab Events

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            var playlistName = Microsoft.VisualBasic.Interaction.InputBox(
                "نام پلی‌لیست جدید:",
                "ایجاد پلی‌لیست",
                "پلی‌لیست من"
            );

            if (!string.IsNullOrEmpty(playlistName))
            {
                _playlistService.CreatePlaylist(playlistName);
                PlaylistsListBox.ItemsSource = _playlistService.GetPlaylists();
                MessageBox.Show("پلی‌لیست جدید ایجاد شد.", "موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ViewPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlaylistItem playlist)
            {
                var dialog = new PlaylistViewDialog(playlist, _playlistService);
                dialog.ShowDialog();

                // Refresh
                PlaylistsListBox.ItemsSource = _playlistService.GetPlaylists();
            }
        }

        private void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlaylistItem playlist)
            {
                var result = MessageBox.Show(
                    $"آیا مطمئن هستید که می‌خواهید پلی‌لیست '{playlist.Name}' را حذف کنید؟",
                    "تأیید حذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    _playlistService.DeletePlaylist(playlist);
                    PlaylistsListBox.ItemsSource = _playlistService.GetPlaylists();
                }
            }
        }

        private void PlaylistsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Handle playlist selection
        }

        #endregion

        #region History Tab Events

        private void PlayFromHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is VideoItem video)
            {
                // Update last played
                _historyService.AddToHistory(video);
                HistoryListBox.ItemsSource = _historyService.GetHistory();

                // Open player
                var playerWindow = new VideoPlayerWindow(video.VideoUrl, video.Title, video.SubtitleUrl);
                playerWindow.Show();
            }
        }

        private void RemoveFromHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is VideoItem video)
            {
                _historyService.RemoveFromHistory(video);
                HistoryListBox.ItemsSource = _historyService.GetHistory();
            }
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var result = CustomMessageBox.ShowQuestion("آیا مطمئن هستید که می‌خواهید تمام تاریخچه را پاک کنید؟", "تأیید پاک کردن");

            if (result == CustomMessageBox.MessageResult.Yes)
            {
                _historyService.ClearHistory();
                HistoryListBox.ItemsSource = _historyService.GetHistory();
            }
        }

        #endregion

        #region Download Management

        private async Task StartDownloadAsync(string videoUrl, string savePath, string fileName, string? subtitleUrl)
        {
            try
            {
                // Create download task
                var videoFileName = DownloadService.GenerateFileName(videoUrl, fileName);
                var videoFilePath = Path.Combine(savePath, videoFileName);

                _currentDownload = new DownloadTask
                {
                    FileName = videoFileName,
                    Url = videoUrl,
                    SavePath = videoFilePath
                };

                // Show download panel
                DownloadPanel.Visibility = Visibility.Visible;
                DownloadFileNameText.Text = videoFileName;

                // Bind download progress
                _currentDownload.PropertyChanged += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (e.PropertyName == nameof(DownloadTask.Progress))
                        {
                            DownloadProgressBar.Value = _currentDownload.Progress;
                            DownloadProgressText.Text = _currentDownload.ProgressText;
                        }
                        else if (e.PropertyName == nameof(DownloadTask.DownloadSpeed))
                        {
                            DownloadSpeedText.Text = _currentDownload.SpeedText;
                        }
                        else if (e.PropertyName == nameof(DownloadTask.DownloadedBytes))
                        {
                            DownloadSizeText.Text = _currentDownload.DownloadedText;
                        }
                    });
                };

                // Start download
                _downloadCancellationTokenSource = new CancellationTokenSource();
                var success = await _downloadService.DownloadFileAsync(
                    videoUrl,
                    videoFilePath,
                    _currentDownload,
                    _downloadCancellationTokenSource.Token
                );

                if (success)
                {
                    MessageBox.Show(
                        $"دانلود با موفقیت تکمیل شد:\n{videoFilePath}",
                        "دانلود تکمیل شد",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Download subtitle if exists
                    if (!string.IsNullOrEmpty(subtitleUrl))
                    {
                        await DownloadSubtitleAsync(subtitleUrl, savePath);
                    }
                }

                // Hide download panel after a delay
                await Task.Delay(2000);
                DownloadPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در دانلود: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                DownloadPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async Task DownloadSubtitleAsync(string subtitleUrl, string savePath)
        {
            try
            {
                var subtitleFileName = DownloadService.GenerateFileName(subtitleUrl);
                var subtitleFilePath = Path.Combine(savePath, subtitleFileName);

                var subtitleDownload = new DownloadTask
                {
                    FileName = subtitleFileName,
                    Url = subtitleUrl,
                    SavePath = subtitleFilePath
                };

                var cts = new CancellationTokenSource();
                await _downloadService.DownloadFileAsync(
                    subtitleUrl,
                    subtitleFilePath,
                    subtitleDownload,
                    cts.Token
                );
            }
            catch
            {
                // Ignore subtitle download errors
            }
        }

        private void CancelDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            _downloadCancellationTokenSource?.Cancel();
            DownloadPanel.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}