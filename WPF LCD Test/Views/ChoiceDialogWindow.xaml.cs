using System.Windows;

namespace WPF_LCD_Test.Views
    {
    /// <summary>
    /// Modal dialog offering two mutually exclusive options; closing it without a choice yields no result.
    /// </summary>
    public partial class ChoiceDialogWindow : Window
        {
        /// <summary>
        /// Gets the zero-based index of the chosen option, or null when the dialog was closed without a choice.
        /// </summary>
        public int? SelectedOption { get; private set; }

        /// <summary>
        /// Initializes the dialog.
        /// </summary>
        /// <param name="message">Message shown above the buttons.</param>
        /// <param name="caption">Window title.</param>
        /// <param name="firstOption">Caption of the first button.</param>
        /// <param name="secondOption">Caption of the second button.</param>
        public ChoiceDialogWindow(string message, string caption, string firstOption, string secondOption)
            {
            InitializeComponent();
            Title = caption;
            MessageText.Text = message;
            FirstButton.Content = firstOption;
            SecondButton.Content = secondOption;
            }

        private void FirstButton_Click(object sender, RoutedEventArgs e) => Choose(0);

        private void SecondButton_Click(object sender, RoutedEventArgs e) => Choose(1);

        private void Choose(int option)
            {
            SelectedOption = option;
            DialogResult = true;
            }
        }
    }
