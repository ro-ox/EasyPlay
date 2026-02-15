using System.Windows;
using System.Windows.Media;

namespace EasyPlay.Helpers
{
    public partial class CustomMessageBox : Window
    {
        public enum MessageType
        {
            Info,
            Success,
            Warning,
            Error,
            Question
        }

        public enum MessageButtons
        {
            Ok,
            OkCancel,
            YesNo,
            YesNoCancel
        }

        public enum MessageResult
        {
            None,
            Ok,
            Cancel,
            Yes,
            No
        }

        public MessageResult Result { get; private set; } = MessageResult.None;

        private CustomMessageBox()
        {
            InitializeComponent();
        }

        #region Static Show Methods


        public static MessageResult Show(string message, string title = "پیام", MessageType type = MessageType.Info)
        {
            return ShowDialog(message, title, type, MessageButtons.Ok);
        }


        public static MessageResult Show(string message, string title, MessageType type, MessageButtons buttons)
        {
            return ShowDialog(message, title, type, buttons);
        }


        public static MessageResult ShowQuestion(string message, string title = "تایید")
        {
            return ShowDialog(message, title, MessageType.Question, MessageButtons.YesNo);
        }

        public static void ShowSuccess(string message, string title = "موفقیت")
        {
            ShowDialog(message, title, MessageType.Success, MessageButtons.Ok);
        }

        public static void ShowError(string message, string title = "خطا")
        {
            ShowDialog(message, title, MessageType.Error, MessageButtons.Ok);
        }

        public static void ShowWarning(string message, string title = "هشدار")
        {
            ShowDialog(message, title, MessageType.Warning, MessageButtons.Ok);
        }

        public static MessageResult ShowWarningQuestion(string message, string title = "تایید")
        {
            return ShowDialog(message, title, MessageType.Warning, MessageButtons.YesNo);
        }

        public static void ShowInfo(string message, string title = "اطلاعات")
        {
            ShowDialog(message, title, MessageType.Info, MessageButtons.Ok);
        }

        #endregion

        private static MessageResult ShowDialog(string message, string title, MessageType type, MessageButtons buttons)
        {
            var msgBox = new CustomMessageBox();
            msgBox.Owner = Application.Current.MainWindow;
            msgBox.ConfigureMessage(message, title, type, buttons);
            msgBox.ShowDialog();
            return msgBox.Result;
        }

        private void ConfigureMessage(string message, string title, MessageType type, MessageButtons buttons)
        {
            TitleText.Text = title;
            MessageText.Text = message;

            // Configure icon and colors based on type
            switch (type)
            {
                case MessageType.Success:
                    MessageIcon.Text = "\uf058";
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#45b7aa"));
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f3d2d"));
                    break;

                case MessageType.Error:
                    MessageIcon.Text = "\uf057";
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff6b6b"));
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3d1f1f"));
                    break;

                case MessageType.Warning:
                    MessageIcon.Text = "\uf071";
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffd93d"));
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3d3d1f"));
                    break;

                case MessageType.Question:
                    MessageIcon.Text = "\uf059";
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff1313"));
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2d1f2d"));
                    break;

                case MessageType.Info:
                default:
                    MessageIcon.Text = "\uf05a";
                    MessageIcon.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b9fff"));
                    IconBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1f2d3d"));
                    break;
            }

            // Configure buttons
            switch (buttons)
            {
                case MessageButtons.Ok:
                    PrimaryButtonText.Text = "تایید";
                    SecondaryButton.Visibility = Visibility.Collapsed;
                    break;

                case MessageButtons.OkCancel:
                    PrimaryButtonText.Text = "تایید";
                    SecondaryButtonText.Text = "انصراف";
                    SecondaryButton.Visibility = Visibility.Visible;
                    break;

                case MessageButtons.YesNo:
                    PrimaryButtonText.Text = "بله";
                    SecondaryButtonText.Text = "خیر";
                    SecondaryButton.Visibility = Visibility.Visible;
                    break;

                case MessageButtons.YesNoCancel:
                    PrimaryButtonText.Text = "بله";
                    SecondaryButtonText.Text = "خیر";
                    SecondaryButton.Visibility = Visibility.Visible;
                    // TODO: Add third button if needed
                    break;
            }
        }

        private void PrimaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = PrimaryButtonText.Text switch
            {
                "بله" => MessageResult.Yes,
                "تایید" => MessageResult.Ok,
                _ => MessageResult.Ok
            };
            Close();
        }

        private void SecondaryButton_Click(object sender, RoutedEventArgs e)
        {
            Result = SecondaryButtonText.Text switch
            {
                "خیر" => MessageResult.No,
                "انصراف" => MessageResult.Cancel,
                _ => MessageResult.Cancel
            };
            Close();
        }
    }
}