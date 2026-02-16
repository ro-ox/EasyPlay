using System.Windows;
using EasyPlay.Helpers;
using System.Windows.Input;
using EasyPlay.Models;

namespace EasyPlay.Views
{
    public partial class PlaylistSelectionDialog : Window
    {
        public PlaylistItem? SelectedPlaylist { get; private set; }

        public PlaylistSelectionDialog(List<PlaylistItem> playlists)
        {
            InitializeComponent();
            PlaylistsComboBox.ItemsSource = playlists;
            if (playlists.Any())
            {
                PlaylistsComboBox.SelectedIndex = 0;
            }
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedPlaylist = PlaylistsComboBox.SelectedItem as PlaylistItem;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
