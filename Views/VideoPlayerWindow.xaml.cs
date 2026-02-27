using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LibVLCSharp.Shared;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using System.Windows.Media.Animation;
using EasyPlay.Helpers;
using LibVLCSharp.WPF;
using System.Diagnostics;

namespace EasyPlay.Views
{
    public enum VideoRenderer
    {
        Auto,
        DirectX,
        DirectDraw,
        Software
    }

    public partial class VideoPlayerWindow : Window
    {
        private LibVLC? _libVLC;
        private MediaPlayer? _mediaPlayer;
        private Media? _media;
        private DispatcherTimer? _timer;
        private DispatcherTimer? _hideControlsTimer;
        private DispatcherTimer _seekOverlayTimer = new DispatcherTimer();
        private DispatcherTimer _volumeOverlayTimer = new();
        private bool _isDragging;
        private bool _isPlaying;
        private string? _videoUrl;
        private string? _subtitleUrl;
        private string? _subtitleTempPath;
        private string? _fontTempPath;
        private long _subtitleDelay;
        private int _currentSpuTrack = -1;
        private bool _isFullscreen;
        private int _subtitleSize = 28;
        private string _subtitleColor = "#FFFFFF";
        private bool _isReinitializing;
        private readonly float[] _speedOptions = { 0.25f, 0.5f, 0.75f, 1f, 1.25f, 1.5f, 2f };
        private int _currentSpeedIndex = 3; // 1x
        private bool _isClosing = false;
        private VideoRenderer _videoRenderer = VideoRenderer.Auto;
        private bool _isNetworkError = false;
        private int _retryCount = 0;
        private const int MAX_RETRIES = 5;
        private long _lastKnownTime = 0;
        private DispatcherTimer? _retryTimer;

        public VideoPlayerWindow(string videoUrl, string title = "پخش ویدیو")
            : this(videoUrl, title, null) { }

        public VideoPlayerWindow(string videoUrl, string title, string? subtitleUrl)
        {
            InitializeComponent();
            Title = title;
            _videoUrl = videoUrl;
            _subtitleUrl = subtitleUrl;
            LoadingOverlay.Visibility = Visibility.Visible;
            SetupHideControlsTimer();
            //Loaded += async (s, e) => await InitializePlayerAsync(videoUrl);
            this.Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(100);
            await InitializePlayerAsync(_videoUrl!);
        }

        #region Custom Title Bar

        // Default Size
        private double _defaultWidth = 1050;
        private double _defaultHeight = 650;

        // Drag
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
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

        // Minimize
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // Maximize/Restore
        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        // Toggle Maximize
        private void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
            UpdateMaximizeIcon(this.WindowState == WindowState.Maximized);
        }

        // Back Default
        private void RestoreDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Width = _defaultWidth;
            this.Height = _defaultHeight;

            // Center
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = (SystemParameters.PrimaryScreenHeight - this.Height) / 2;

            UpdateMaximizeIcon(false);
        }

        // Close
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Update Icon Maximize
        private void UpdateMaximizeIcon(bool isMaximized)
        {
            if (MaximizeIcon != null)
            {
                MaximizeIcon.Text = isMaximized ? "\uf2d2" : "\uf2d0";
            }
        }

        // State
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            UpdateMaximizeIcon(this.WindowState == WindowState.Maximized);
        }

        #endregion

        private string? ExtractFontToTemp()
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "kargadan_font.ttf");
                if (File.Exists(tempPath)) return tempPath;

                var uriFormats = new[]
                {
                    "pack://application:,,,/Assets/Fonts/Vazirmatn.ttf",
                    "pack://application:,,,/EasyPlay;component/Assets/Fonts/Vazirmatn.ttf"
                };

                foreach (var uriString in uriFormats)
                {
                    try
                    {
                        var uri = new Uri(uriString, UriKind.Absolute);
                        var resourceInfo = Application.GetResourceStream(uri);
                        if (resourceInfo != null)
                        {
                            using var fileStream = File.Create(tempPath);
                            resourceInfo.Stream.CopyTo(fileStream);
                            return tempPath;
                        }
                    }
                    catch { }
                }
                return null;
            }
            catch { return null; }
        }

        private void SetupHideControlsTimer()
        {
            _hideControlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _hideControlsTimer.Tick += (s, e) =>
            {
                if (_isFullscreen && _isPlaying)
                {
                    ControlsPanel.Visibility = Visibility.Collapsed;
                    Mouse.OverrideCursor = Cursors.None;
                }
                _hideControlsTimer.Stop();
            };
        }

        private List<string> BuildVlcOptions()
        {
            var options = new List<string>
            {
                "--subsdec-encoding=UTF-8",
                $"--freetype-fontsize={_subtitleSize}",
                "--freetype-bold",
                "--freetype-outline-thickness=2",
                "--freetype-outline-color=0",
                "--sub-text-scale=100",
                "--text-renderer=freetype",
                "--no-sub-autodetect-file",
                $"--freetype-color={ColorToVlcInt(_subtitleColor)}"
            };

            // Video Renderer
            switch (_videoRenderer)
            {
                case VideoRenderer.DirectX:
                    options.Add("--vout=direct3d9");
                    options.Add("--avcodec-hw=d3d11va");  // Hardware decode
                    break;

                case VideoRenderer.DirectDraw:
                    options.Add("--vout=directdraw");
                    options.Add("--avcodec-hw=dxva2");  // Hardware decode
                    break;

                case VideoRenderer.Software:
                    options.Add("--avcodec-hw=none");      // Software decode
                    options.Add("--vout=win32");           // Software render
                    options.Add("--no-directx-hw-yuv");    // Disable GPU YUV
                    break;
            }

            _fontTempPath = ExtractFontToTemp();
            options.Add(!string.IsNullOrEmpty(_fontTempPath) && File.Exists(_fontTempPath)
                ? "--freetype-font=Vazirmatn"
                : "--freetype-font=Tahoma");

            return options;
        }

        private async Task InitializePlayerAsync(string videoUrl)
        {
            try
            {
                await Task.Run(() =>
                {
                    Core.Initialize();

                    Dispatcher.Invoke(() =>
                    {
                        _libVLC = new LibVLC(BuildVlcOptions().ToArray());
                        _mediaPlayer = new MediaPlayer(_libVLC);
                        _mediaPlayer.EnableMouseInput = false;
                        _mediaPlayer.EnableKeyInput = false;
                        _mediaPlayer.Volume = 100;
                        VideoView.MediaPlayer = _mediaPlayer;

                        _mediaPlayer.Playing += MediaPlayer_Playing;
                        _mediaPlayer.Paused += MediaPlayer_Paused;
                        _mediaPlayer.Stopped += MediaPlayer_Stopped;
                        _mediaPlayer.EndReached += MediaPlayer_EndReached;
                        _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
                        _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;

                        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                        _timer.Tick += Timer_Tick;

                        _media = new Media(_libVLC, new Uri(videoUrl));
                        _mediaPlayer.Play(_media);
                        _timer.Start();
                    });
                });

                if (!string.IsNullOrEmpty(_subtitleUrl))
                    await LoadSubtitleAsync(_subtitleUrl);

                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                CustomMessageBox.ShowError($"خطا در بارگذاری ویدیو", "خطا");
                Debug.WriteLine($"Error Loading Video: {ex}");
                Close();
            }
        }

        private void ReinitializePlayer()
        {
            if (_mediaPlayer == null || _videoUrl == null || _isReinitializing) return;

            _isReinitializing = true;
            LoadingText.Text = "در حال اعمال تغییرات...";
            LoadingOverlay.Visibility = Visibility.Visible;
            ControlsPanel.IsEnabled = false;

            var currentTime = _mediaPlayer.Time;
            var wasPlaying = _isPlaying;

            _timer?.Stop();
            _mediaPlayer.Stop();
            _mediaPlayer.Dispose();
            _media?.Dispose();
            _libVLC?.Dispose();

            _libVLC = new LibVLC(BuildVlcOptions().ToArray());
            _mediaPlayer = new MediaPlayer(_libVLC);
            _mediaPlayer.EnableMouseInput = false;
            _mediaPlayer.EnableKeyInput = false;
            _mediaPlayer.Volume = 100;
            VideoView.MediaPlayer = _mediaPlayer;

            _mediaPlayer.Playing += MediaPlayer_Playing;
            _mediaPlayer.Paused += MediaPlayer_Paused;
            _mediaPlayer.Stopped += MediaPlayer_Stopped;
            _mediaPlayer.EndReached += MediaPlayer_EndReached;
            _mediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
            _mediaPlayer.LengthChanged += MediaPlayer_LengthChanged;

            _media = new Media(_libVLC, new Uri(_videoUrl));
            _mediaPlayer.Play(_media);

            Task.Delay(500).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    _mediaPlayer.Time = currentTime;
                    if (!wasPlaying) _mediaPlayer.Pause();

                    if (!string.IsNullOrEmpty(_subtitleTempPath) && File.Exists(_subtitleTempPath))
                    {
                        _mediaPlayer.AddSlave(MediaSlaveType.Subtitle,
                            $"file:///{_subtitleTempPath.Replace("\\", "/")}", true);
                    }

                    _timer?.Start();
                    ControlsPanel.IsEnabled = true;
                    LoadingText.Text = "در حال بارگذاری...";
                    _isReinitializing = false;
                });
            });
        }

        private int ColorToVlcInt(string hexColor)
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            return (color.R << 16) | (color.G << 8) | color.B;
        }

        private async Task LoadSubtitleAsync(string subtitleUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                //httpClient.DefaultRequestHeaders.Add("X-API-Key", AppConfig.ApiKey);
                var subtitleContent = await httpClient.GetStringAsync(subtitleUrl);
                _subtitleTempPath = Path.Combine(Path.GetTempPath(), $"kargadan_sub_{Guid.NewGuid()}.vtt");
                await File.WriteAllTextAsync(_subtitleTempPath, subtitleContent);

                await Task.Delay(1500);
                Dispatcher.Invoke(() =>
                {
                    _mediaPlayer?.AddSlave(MediaSlaveType.Subtitle,
                        $"file:///{_subtitleTempPath.Replace("\\", "/")}", true);

                    Task.Delay(500).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (_mediaPlayer?.SpuCount > 0)
                                _currentSpuTrack = _mediaPlayer.Spu;
                        });
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading subtitle: {ex.Message}");
            }
        }

        private void ShowSeekOverlay(long deltaMs)
        {
            _seekOverlayTimer.Stop();

            var seconds = Math.Abs(deltaMs) / 1000;
            var sign = deltaMs > 0 ? "⏩ " : "⏪ ";

            SeekOverlayText.Text = $"{sign}{seconds} ثانیه";
            SeekOverlayPopup.IsOpen = true;

            _seekOverlayTimer.Interval = TimeSpan.FromSeconds(1.5);
            _seekOverlayTimer.Tick -= SeekOverlayTimer_Tick;
            _seekOverlayTimer.Tick += SeekOverlayTimer_Tick;
            _seekOverlayTimer.Start();
        }

        private void SeekOverlayTimer_Tick(object? sender, EventArgs e)
        {
            _seekOverlayTimer.Stop();
            SeekOverlayPopup.IsOpen = false;
        }


        //private void FadeOutSeekOverlay()
        //{
        //    var anim = new DoubleAnimation
        //    {
        //        From = 1,
        //        To = 0,
        //        Duration = TimeSpan.FromMilliseconds(250),
        //        FillBehavior = FillBehavior.Stop
        //    };

        //    anim.Completed += (_, __) =>
        //    {
        //        SeekOverlay.Visibility = Visibility.Collapsed;
        //        SeekOverlay.Opacity = 1;
        //    };

        //    SeekOverlay.BeginAnimation(UIElement.OpacityProperty, anim);
        //}



        #region MediaPlayer Events

        private void MediaPlayer_Playing(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isPlaying = true;
                if (PlayPauseButton.Content is TextBlock tb)
                {
                    tb.Text = "\uf04c"; // Pause
                }

                LoadingOverlay.Visibility = Visibility.Collapsed;

                if (_isNetworkError)
                {
                    _retryCount = 0;
                    _isNetworkError = false;
                    LoadingText.Text = "در حال بارگذاری...";
                }

                ResetHideControlsTimer();
            });
        }

        private void MediaPlayer_Paused(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isPlaying = false;
                if (PlayPauseButton.Content is TextBlock tb)
                {
                    tb.Text = "\uf04b;"; // Play
                }
                ShowControls();
            });
        }

        private void MediaPlayer_Stopped(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isPlaying = false;
                if (PlayPauseButton.Content is TextBlock tb)
                {
                    tb.Text = "\uf04b;"; // Play
                }
                ProgressSlider.Value = 0;
                ShowControls();
            });
        }

        private void MediaPlayer_EndReached(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _isPlaying = false;
                if (PlayPauseButton.Content is TextBlock tb)
                {
                    tb.Text = "\uf04b;"; // Play
                }
                ProgressSlider.Value = 0;
                ShowControls();
            });
        }

        private void MediaPlayer_EncounteredError(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _lastKnownTime = _mediaPlayer?.Time ?? 0;

                if (_retryCount < MAX_RETRIES)
                {
                    _isNetworkError = true;
                    LoadingOverlay.Visibility = Visibility.Visible;
                    LoadingText.Text = $"در حال تلاش مجدد... ({_retryCount + 1}/{MAX_RETRIES})";

                    _retryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    _retryTimer.Tick += RetryConnection;
                    _retryTimer.Start();

                    _retryCount++;
                }
                else
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    _retryCount = 0;
                    _isNetworkError = false;

                    var result = CustomMessageBox.ShowQuestion(
                        "خطا در پخش ویدیو\n\nآیا می‌خواهید دوباره تلاش کنید؟",
                        "خطا در پخش");

                    if (result == CustomMessageBox.MessageResult.Yes)
                    {
                        RetryConnection(null, null);
                    }
                }
            });
        }

        private void RetryConnection(object? sender, EventArgs? e)
        {
            _retryTimer?.Stop();

            try
            {
                LoadingText.Text = "در حال اتصال مجدد...";

                var savedTime = _lastKnownTime;

                _mediaPlayer?.Stop();
                _media?.Dispose();

                _media = new Media(_libVLC!, new Uri(_videoUrl!));
                _mediaPlayer?.Play(_media);

                Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (_mediaPlayer != null && savedTime > 0)
                        {
                            _mediaPlayer.Time = savedTime;
                        }

                        _retryCount = 0;
                        _isNetworkError = false;
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Retry failed: {ex.Message}");

                if (_retryCount < MAX_RETRIES)
                {
                    _retryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    _retryTimer.Tick += RetryConnection;
                    _retryTimer.Start();
                    _retryCount++;
                }
            }
        }

        private void MediaPlayer_LengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
        {
            Dispatcher.Invoke(() => ProgressSlider.Maximum = e.Length);
        }

        #endregion

        #region Timer

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_mediaPlayer == null || _isDragging) return;

            var currentTime = _mediaPlayer.Time;
            var totalTime = _mediaPlayer.Length;

            if (totalTime > 0)
            {
                ProgressSlider.Value = currentTime;
                var current = TimeSpan.FromMilliseconds(currentTime);
                var total = TimeSpan.FromMilliseconds(totalTime);
                TimeText.Text = $"{current:hh\\:mm\\:ss} / {total:hh\\:mm\\:ss}";
            }
        }

        #endregion

        #region Controls Visibility

        private void ShowControls()
        {
            ControlsPanel.Visibility = Visibility.Visible;
            Mouse.OverrideCursor = null;
            ResetHideControlsTimer();
        }

        private void ResetHideControlsTimer()
        {
            _hideControlsTimer?.Stop();
            if (_isFullscreen && _isPlaying)
                _hideControlsTimer?.Start();
        }

        private void VideoView_MouseMove(object sender, MouseEventArgs e) => ShowControls();

        private void VideoView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PlayPause_Click(sender, e);
        }

        private void VideoView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleFullscreen();
        }

        #endregion

        #region Control Events

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;
            if (_isPlaying) _mediaPlayer.Pause();
            else _mediaPlayer.Play();
        }

        private void Stop_Click(object sender, RoutedEventArgs e) => _mediaPlayer?.Stop();

        private void ProgressSlider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0)
                return;

            _isDragging = true;
            _timer?.Stop();

            var slider = (Slider)sender;
            slider.CaptureMouse();

            SetSliderByMouse(slider, e.GetPosition(slider).X);
        }


        private void ProgressSlider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mediaPlayer == null) return;

            var slider = (Slider)sender;
            slider.ReleaseMouseCapture();

            _mediaPlayer.Time = (long)slider.Value;
            _isDragging = false;

            _timer?.Start();
        }


        private void ProgressSlider_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mediaPlayer == null || _mediaPlayer.Length <= 0)
                return;

            var slider = (Slider)sender;
            var mouseX = e.GetPosition(slider).X;

            UpdateProgressTooltip(slider, mouseX);

            if (_isDragging)
                SetSliderByMouse(slider, mouseX);
        }

        private void UpdateProgressTooltip(Slider slider, double mouseX)
        {
            var ratio = Math.Clamp(mouseX / slider.ActualWidth, 0, 1);
            var timeMs = (long)(_mediaPlayer!.Length * ratio);
            var time = TimeSpan.FromMilliseconds(timeMs);

            ProgressTooltipText.Text = time.ToString(@"hh\:mm\:ss");

            var sliderPos = slider.PointToScreen(new Point(0, 0));
            ProgressTooltipPopup.HorizontalOffset = sliderPos.X + mouseX - 30;
            ProgressTooltipPopup.VerticalOffset = sliderPos.Y - 40;

            ProgressTooltipPopup.IsOpen = true;
        }



        private void SetSliderByMouse(Slider slider, double mouseX)
        {
            var ratio = Math.Clamp(mouseX / slider.ActualWidth, 0, 1);
            slider.Value = _mediaPlayer!.Length * ratio;
        }



        private void ProgressSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            ProgressTooltipPopup.IsOpen = false;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_mediaPlayer == null) return;

            _mediaPlayer.Volume = (int)e.NewValue;
            ShowVolumeOverlay((int)e.NewValue);
        }


        private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (VolumeSlider.IsMouseOver || ControlsPanel.IsMouseOver)
                return;

            const int step = 5;

            if (e.Delta > 0)
                VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + step);
            else
                VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - step);

            //ShowVolumeOverlay((int)VolumeSlider.Value);
        }


        private void ShowVolumeOverlay(int volume)
        {
            _volumeOverlayTimer.Stop();

            VolumeOverlayText.Text = $"{volume}%";

            if (volume == 0)
                VolumeIcon.Text = "\uf026";
            else if (volume < 50)
                VolumeIcon.Text = "\uf027";
            else
                VolumeIcon.Text = "\uf028";

            VolumeOverlayPopup.IsOpen = true;

            _volumeOverlayTimer.Interval = TimeSpan.FromSeconds(1.5);
            _volumeOverlayTimer.Tick -= VolumeOverlayTimer_Tick;
            _volumeOverlayTimer.Tick += VolumeOverlayTimer_Tick;
            _volumeOverlayTimer.Start();
        }

        private void VolumeOverlayTimer_Tick(object? sender, EventArgs e)
        {
            _volumeOverlayTimer.Stop();
            VolumeOverlayPopup.IsOpen = false;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.XButton1)
            {
                SeekBy(-10000);
                e.Handled = true;
            }
            else if (e.ChangedButton == MouseButton.XButton2)
            {
                SeekBy(10000);
                e.Handled = true;
            }
        }

        private void SeekBy(long deltaMs)
        {
            if (_mediaPlayer != null)
            {
                var newTime = Math.Clamp(
                _mediaPlayer.Time + deltaMs,
                0,
                _mediaPlayer.Length);

                _mediaPlayer.Time = newTime;
                ShowSeekOverlay(deltaMs);
            }
        }


        private void SpeedDown_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;

            if (_currentSpeedIndex > 0)
            {
                _currentSpeedIndex--;
                ApplySpeed();
            }
        }

        private void SpeedUp_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;

            if (_currentSpeedIndex < _speedOptions.Length - 1)
            {
                _currentSpeedIndex++;
                ApplySpeed();
            }
        }

        private void ApplySpeed()
        {
            var speed = _speedOptions[_currentSpeedIndex];
            _mediaPlayer?.SetRate(speed);
            SpeedText.Text = speed == 1f ? "1x" : $"{speed}x";
        }

        private void SubtitleDelayMinus_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;
            _subtitleDelay -= 500000;
            _mediaPlayer.SetSpuDelay(_subtitleDelay);
            UpdateSubtitleDelayText();
        }

        private void SubtitleDelayPlus_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;
            _subtitleDelay += 500000;
            _mediaPlayer.SetSpuDelay(_subtitleDelay);
            UpdateSubtitleDelayText();
        }

        private void UpdateSubtitleDelayText()
        {
            var seconds = _subtitleDelay / 1000000.0;
            SubtitleDelayText.Text = $"{seconds:+0.0;-0.0;0.0}s";
        }

        private void SubtitleSizeDown_Click(object sender, RoutedEventArgs e)
        {
            if (_subtitleSize > 16)
            {
                _subtitleSize -= 4;
                SubtitleSizeText.Text = _subtitleSize.ToString();
                ReinitializePlayer();
            }
        }

        private void SubtitleSizeUp_Click(object sender, RoutedEventArgs e)
        {
            if (_subtitleSize < 60)
            {
                _subtitleSize += 4;
                SubtitleSizeText.Text = _subtitleSize.ToString();
                ReinitializePlayer();
            }
        }

        private void SubtitleColor_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerPopup.IsOpen = !ColorPickerPopup.IsOpen;
        }

        private void ColorOption_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string colorHex)
            {
                _subtitleColor = colorHex;
                SubtitleColorIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
                ColorPickerPopup.IsOpen = false;
                ReinitializePlayer();
            }
        }

        private void SubtitleToggle_Click(object sender, RoutedEventArgs e)
        {
            if (_mediaPlayer == null) return;

            Task.Run(() =>
            {
                var currentSpu = _mediaPlayer.Spu;

                if (currentSpu == -1)
                {
                    if (_currentSpuTrack > 0)
                        _mediaPlayer.SetSpu(_currentSpuTrack);
                    else if (_mediaPlayer.SpuCount > 0)
                    {
                        var tracks = _mediaPlayer.SpuDescription;
                        foreach (var track in tracks)
                        {
                            if (track.Id != -1)
                            {
                                _mediaPlayer.SetSpu(track.Id);
                                _currentSpuTrack = track.Id;
                                break;
                            }
                        }
                    }

                    Dispatcher.Invoke(() =>
                        SubtitleToggleButton.Background = new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)));
                }
                else
                {
                    _currentSpuTrack = currentSpu;
                    _mediaPlayer.SetSpu(-1);

                    Dispatcher.Invoke(() =>
                        SubtitleToggleButton.Background = new SolidColorBrush(Color.FromRgb(0x80, 0x00, 0x00)));
                }
            });
        }

        private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
            {
                Topmost = false;
                WindowState = WindowState.Normal;
                TitleBarCustom.Visibility = Visibility.Visible;
                //WindowStyle = WindowStyle.SingleBorderWindow;
                _isFullscreen = false;
                ShowControls();
                _hideControlsTimer?.Stop();
            }
            else
            {
                TitleBarCustom.Visibility = Visibility.Collapsed;
                //WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
                Topmost = true;
                _isFullscreen = true;
                ResetHideControlsTimer();
            }
        }

        private void RenderMode_Click(object sender, RoutedEventArgs e)
        {
            _videoRenderer = _videoRenderer switch
            {
                VideoRenderer.Auto => VideoRenderer.DirectX,
                VideoRenderer.DirectX => VideoRenderer.DirectDraw,
                VideoRenderer.DirectDraw => VideoRenderer.Software,
                VideoRenderer.Software => VideoRenderer.Auto,
                _ => VideoRenderer.Auto
            };

            var modeName = _videoRenderer switch
            {
                VideoRenderer.Auto => "خودکار (پیش‌فرض)",
                VideoRenderer.DirectX => "DirectX (سخت‌افزاری)",
                VideoRenderer.DirectDraw => "DirectDraw (متوسط)",
                VideoRenderer.Software => "نرم‌افزاری (کند)",
                _ => "خودکار"
            };

            CustomMessageBox.ShowInfo(
                $"حالت رندرینگ: {modeName}\n\nویدیو مجدداً بارگذاری می‌شود.",
                "تغییر حالت رندرینگ");

            ReinitializePlayer();
        }

        #endregion

        #region Keyboard Shortcuts

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_mediaPlayer == null) return;

            long delta;

            switch (e.Key)
            {
                case Key.Space:
                    ShowControls();
                    PlayPause_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.Escape:
                    ShowControls();
                    if (_isFullscreen) ToggleFullscreen();
                    else Close();
                    e.Handled = true;
                    break;
                case Key.F:
                case Key.F11:
                    ToggleFullscreen();
                    e.Handled = true;
                    break;
                case Key.Left:
                    delta = -10000;
                    ShowSeekOverlay(delta);
                    _mediaPlayer.Time = Math.Max(0, _mediaPlayer.Time + delta);
                    e.Handled = true;
                    break;
                case Key.Right:
                    delta = 10000;
                    ShowSeekOverlay(delta);
                    _mediaPlayer.Time = Math.Min(_mediaPlayer.Length, _mediaPlayer.Time + delta);
                    e.Handled = true;
                    break;
                case Key.Up:
                    VolumeSlider.Value = Math.Min(100, VolumeSlider.Value + 5);
                    ShowVolumeOverlay((int)VolumeSlider.Value);
                    e.Handled = true;
                    break;
                case Key.Down:
                    VolumeSlider.Value = Math.Max(0, VolumeSlider.Value - 5);
                    ShowVolumeOverlay((int)VolumeSlider.Value);
                    e.Handled = true;
                    break;
                case Key.M:
                    _mediaPlayer.Mute = !_mediaPlayer.Mute;
                    e.Handled = true;
                    break;
                case Key.OemOpenBrackets:
                    SubtitleDelayMinus_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.OemCloseBrackets:
                    SubtitleDelayPlus_Click(sender, e);
                    e.Handled = true;
                    break;
                case Key.C:
                    SubtitleToggle_Click(sender, e);
                    e.Handled = true;
                    break;
            }
        }

        #endregion

        #region Window Events

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ShowControls();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();

            if (_isClosing) return;
            _isClosing = true;

            e.Cancel = true;

            Task.Run(async () =>
            {
                await Task.Delay(100);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _timer?.Stop();
                    _hideControlsTimer?.Stop();
                    _seekOverlayTimer?.Stop();
                    _volumeOverlayTimer?.Stop();
                    _retryTimer?.Stop();
                });

                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Playing -= MediaPlayer_Playing;
                    _mediaPlayer.Paused -= MediaPlayer_Paused;
                    _mediaPlayer.Stopped -= MediaPlayer_Stopped;
                    _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                    _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                    _mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;

                    try { _mediaPlayer.Stop(); } catch { }
                    try { _mediaPlayer?.Dispose(); } catch { }
                }

                try { _media?.Dispose(); } catch { }
                try { _libVLC?.Dispose(); } catch { }

                if (!string.IsNullOrEmpty(_subtitleTempPath) && File.Exists(_subtitleTempPath))
                {
                    try { File.Delete(_subtitleTempPath); } catch { }
                }

                if (!string.IsNullOrEmpty(_fontTempPath) && File.Exists(_fontTempPath))
                {
                    try { File.Delete(_fontTempPath); } catch { }
                }

                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.Close();
                    });
                }
                catch { }
            });
        }

        public void ForceClose()
        {
            if (_isClosing) return;
            _isClosing = true;

            try
            {
                _timer?.Stop();
                _hideControlsTimer?.Stop();
                _seekOverlayTimer?.Stop();
                _volumeOverlayTimer?.Stop();

                if (_mediaPlayer != null)
                {
                    _mediaPlayer.Playing -= MediaPlayer_Playing;
                    _mediaPlayer.Paused -= MediaPlayer_Paused;
                    _mediaPlayer.Stopped -= MediaPlayer_Stopped;
                    _mediaPlayer.EndReached -= MediaPlayer_EndReached;
                    _mediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                    _mediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;

                    try { _mediaPlayer.Stop(); } catch { }
                    try { _mediaPlayer?.Dispose(); } catch { }
                }

                try { _media?.Dispose(); } catch { }
                try { _libVLC?.Dispose(); } catch { }
            }
            catch { }

            Dispatcher.Invoke(() =>
            {
                try { Close(); } catch { }
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_isClosing)
            {
                _isClosing = true;

                if (_mediaPlayer != null)
                {
                    try
                    {
                        _mediaPlayer.Stop();
                    }
                    catch { }
                }
            }
        }

        #endregion
    }

    public class SliderWidthConverter : System.Windows.Data.IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length == 3 &&
                values[0] is double value &&
                values[1] is double maximum &&
                values[2] is double width &&
                maximum > 0)
            {
                return (value / maximum) * width;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
}