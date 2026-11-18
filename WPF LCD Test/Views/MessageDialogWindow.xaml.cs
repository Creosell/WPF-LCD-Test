using System.Windows;

namespace WPF_LCD_Test.Views
    {
    /// <summary>
    /// Modal message dialog with OK or Yes/No buttons. Unlike the native message box it runs on the WPF dispatcher,
    /// so animations of the owner window (the button feedback) keep playing while it is open.
    /// </summary>
    public partial class MessageDialogWindow : Window
        {
        private readonly MessageBoxResult _primaryResult;
        private readonly MessageBoxResult _secondaryResult;

        /// <summary>
        /// Gets the button the operator chose; closing the dialog by the window button equals the cancel button.
        /// </summary>
        public MessageBoxResult Result { get; private set; }

        /// <summary>
        /// Initializes the dialog.
        /// </summary>
        /// <param name="message">Message text.</param>
        /// <param name="caption">Window title.</param>
        /// <param name="buttons">OK or YesNo; other sets are shown as OK.</param>
        public MessageDialogWindow(string message, string caption, MessageBoxButton buttons)
            {
            InitializeComponent();
            Title = caption;
            MessageText.Text = message;

            if (buttons == MessageBoxButton.YesNo)
                {
                _primaryResult = MessageBoxResult.Yes;
                _secondaryResult = MessageBoxResult.No;
                PrimaryButton.Content = Caption("DialogYes", "Yes");
                SecondaryButton.Content = Caption("DialogNo", "No");
                }
            else
                {
                _primaryResult = _secondaryResult = MessageBoxResult.OK;
                PrimaryButton.Content = Caption("DialogOk", "OK");
                SecondaryButton.Visibility = Visibility.Collapsed;
                }

            Result = _secondaryResult;
            }

        private static string Caption(string resourceKey, string fallback) =>
            Application.Current?.TryFindResource(resourceKey) is string text ? text.Trim() : fallback;

        private void PrimaryButton_Click(object sender, RoutedEventArgs e) => Close(_primaryResult);

        private void SecondaryButton_Click(object sender, RoutedEventArgs e) => Close(_secondaryResult);

        private void Close(MessageBoxResult result)
            {
            Result = result;
            DialogResult = true;
            }
        }
    }
