using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace EasyPlay.Helpers
{
    public partial class CustomInputBox : Window
    {
        public string InputValue { get; private set; } = string.Empty;
        public bool IsCancelled { get; private set; } = true;

        private int _maxLength = 100;
        private Func<string, bool> _validator;
        private string _validationMessage = string.Empty;

        private CustomInputBox()
        {
            InitializeComponent();
            InputTextBox.Focus();

            // Window load animation
            this.Loaded += (s, e) => PlayWindowAnimation();
        }

        #region Static Show Methods

        /// <summary>
        /// نمایش یک InputBox ساده
        /// </summary>
        public static string Show(string prompt, string title = "ورودی", string defaultValue = "")
        {
            var inputBox = new CustomInputBox();
            inputBox.Owner = Application.Current.MainWindow;
            inputBox.Configure(prompt, title, defaultValue);
            inputBox.ShowDialog();
            return inputBox.IsCancelled ? null : inputBox.InputValue;
        }

        /// <summary>
        /// نمایش InputBox با Validation سفارشی
        /// </summary>
        public static string Show(string prompt, string title, string defaultValue,
            Func<string, bool> validator, string validationMessage = "ورودی نامعتبر است")
        {
            var inputBox = new CustomInputBox();
            inputBox.Owner = Application.Current.MainWindow;
            inputBox._validator = validator;
            inputBox._validationMessage = validationMessage;
            inputBox.Configure(prompt, title, defaultValue);
            inputBox.ShowDialog();
            return inputBox.IsCancelled ? null : inputBox.InputValue;
        }

        /// <summary>
        /// نمایش InputBox با محدودیت طول
        /// </summary>
        public static string Show(string prompt, string title, string defaultValue, int maxLength)
        {
            var inputBox = new CustomInputBox();
            inputBox.Owner = Application.Current.MainWindow;
            inputBox._maxLength = maxLength;
            inputBox.InputTextBox.MaxLength = maxLength;
            inputBox.Configure(prompt, title, defaultValue);
            inputBox.ShowDialog();
            return inputBox.IsCancelled ? null : inputBox.InputValue;
        }

        /// <summary>
        /// نمایش InputBox برای نام پلی لیست (با Validation)
        /// </summary>
        public static string ShowForPlaylistName(string defaultValue = "پلی لیست من")
        {
            return Show(
                "نام پلی لیست جدید:",
                "ایجاد پلی لیست",
                defaultValue,
                ValidatePlaylistName,
                "نام پلی لیست نمی‌تواند خالی باشد"
            );
        }

        /// <summary>
        /// نمایش InputBox برای URL (با Validation)
        /// </summary>
        public static string ShowForUrl(string prompt = "آدرس را وارد کنید:", string title = "ورود URL")
        {
            return Show(
                prompt,
                title,
                "https://",
                ValidateUrl,
                "آدرس وارد شده معتبر نیست"
            );
        }

        #endregion

        #region Validators

        private static bool ValidatePlaylistName(string input)
        {
            return !string.IsNullOrWhiteSpace(input) && input.Length >= 1;
        }

        private static bool ValidateUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult)
                && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        #endregion

        private void Configure(string prompt, string title, string defaultValue)
        {
            TitleText.Text = title;
            PromptText.Text = prompt;
            InputTextBox.Text = defaultValue;
            InputTextBox.SelectAll();

            UpdateCharCounter();
        }

        private void InputTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateCharCounter();

            // Hide validation message when user types
            if (ValidationText.Visibility == Visibility.Visible)
            {
                ValidationText.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateCharCounter()
        {
            int currentLength = InputTextBox.Text.Length;
            CharCountText.Text = $"{currentLength}/{_maxLength}";

            // Change color based on length
            if (currentLength >= _maxLength * 0.9)
            {
                CharCountText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff6b6b"));
            }
            else if (currentLength >= _maxLength * 0.7)
            {
                CharCountText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ffd93d"));
            }
            else
            {
                CharCountText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
            }
        }

        private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter = OK
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                AcceptInput();
            }
            // Escape = Cancel
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                CancelInput();
            }
        }

        private void PrimaryButton_Click(object sender, MouseButtonEventArgs e)
        {
            AcceptInput();
        }

        private void SecondaryButton_Click(object sender, MouseButtonEventArgs e)
        {
            CancelInput();
        }

        private void AcceptInput()
        {
            string input = InputTextBox.Text.Trim();

            // Validation
            if (_validator != null && !_validator(input))
            {
                ShowValidationError(_validationMessage);
                ShakeAnimation();
                return;
            }

            InputValue = input;
            IsCancelled = false;
            Close();
        }

        private void CancelInput()
        {
            IsCancelled = true;
            Close();
        }

        private void ShowValidationError(string message)
        {
            ValidationText.Text = message;
            ValidationText.Visibility = Visibility.Visible;
        }

        #region Animations

        private void PlayWindowAnimation()
        {
            // Fade in animation for Window
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            // Scale animation for MainBorder
            var scaleTransform = MainBorder.RenderTransform as ScaleTransform;
            if (scaleTransform != null)
            {
                scaleTransform.ScaleX = 0.9;
                scaleTransform.ScaleY = 0.9;

                var scaleX = new DoubleAnimation(0.9, 1, TimeSpan.FromSeconds(0.3))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var scaleY = new DoubleAnimation(0.9, 1, TimeSpan.FromSeconds(0.3))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
            }
        }

        private void ShakeAnimation()
        {
            var shake = new DoubleAnimationUsingKeyFrames
            {
                Duration = TimeSpan.FromSeconds(0.4)
            };

            shake.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.1))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(10, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(-10, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.3))));
            shake.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.4))));

            var transform = new TranslateTransform();
            InputTextBox.RenderTransform = transform;
            transform.BeginAnimation(TranslateTransform.XProperty, shake);

            // Flash red border
            var colorAnimation = new ColorAnimation
            {
                To = (Color)ColorConverter.ConvertFromString("#ff6b6b"),
                Duration = TimeSpan.FromSeconds(0.2),
                AutoReverse = true
            };
            InputTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#45b7aa"));
            ((SolidColorBrush)InputTextBox.BorderBrush).BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var storyboard = this.FindResource("ButtonHoverIn") as Storyboard;
                storyboard?.Begin(border);
            }
        }

        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var storyboard = this.FindResource("ButtonHoverOut") as Storyboard;
                storyboard?.Begin(border);
            }
        }

        #endregion
    }
}