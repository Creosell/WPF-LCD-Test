using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_LCD_Test.ViewModels;

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

        private void MeasurementTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel vm && sender is TextBox tb)
            {
                if (int.TryParse(tb.Text, out int value) && value > 0)
                {
                    vm.MeasurementTime = value;
                }
                else
                {
                    // Optionally show error or reset to previous valid value
                    tb.Text = vm.MeasurementTime.ToString();
                }
            }
        }
    }
}