using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WPF_LCD_Test.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Moves the keyboard focus to the view when a drop-down list closes, so the picked value is not left highlighted.
        /// </summary>
        private void ComboBox_DropDownClosed(object sender, EventArgs e) => Focus();

        /// <summary>
        /// Moves the keyboard focus to the view when the click lands outside any input control.
        /// </summary>
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            for (var current = e.OriginalSource as DependencyObject; current != null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is TextBox or ComboBox or Button) return;
            }

            Focus();
        }
    }
}
