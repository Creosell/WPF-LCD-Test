using System.Windows;
using System.Windows.Input;

namespace WPF_LCD_Test.Views
{
    /// <summary>
    /// Main application window with custom title bar controls.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of MainWindow.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles minimize button click to minimize the window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void MinimizeImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Handles maximize/restore button click to toggle window state between maximized and normal.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void MaximizeRestoreImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        /// <summary>
        /// Handles close button click to close the window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void CloseImage_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles title bar mouse down events for window dragging and double-click maximize/restore.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void Grid_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    try
                    {
                        this.DragMove();
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore exception if DragMove cannot be called
                    }
                }
                if (e.ClickCount == 2)
                {
                    MaximizeRestoreImage_PreviewMouseLeftButtonDown(sender, e);
                }
            }
        }
    }
}