using System.Windows;
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
