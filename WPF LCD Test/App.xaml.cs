using System.Diagnostics;
using System.Windows;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.ViewModels;
using WPF_LCD_Test.Views;

namespace WPF_LCD_Test
    {
    public partial class App : Application
        {
        private IColorMeasurementService colorMeasurementService = new ColorMeasurementService();
        private IFileService fileService = new FileService();
        private IDialogService dialogService = new DialogService();
        private ISettingsService settingsService = SettingsService.Instance;
        private ILocalizationService localizationService = LocalizationService.Instance;
        private IDispatcher dispatcher = new WpfDispatcher();
        private IUploadService uploadService = new UploadService();

        /// <summary>
        /// Initializes application services, loads settings, configures localization, and displays main window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Startup event arguments.</param>
        private void Application_Startup(object sender, StartupEventArgs e)
            {
            AppSettings appSettings = settingsService.LoadSettings();

            localizationService.LanguageChanged += LocalizationService_LanguageChanged;
            localizationService.SetLanguage(appSettings.LanguageCultureCode);

            if (int.TryParse(appSettings.ColorAnalyzerChannel, out int channel))
                {
                colorMeasurementService.CurrentChannel = channel;
                }

            MainWindowViewModel mainWindowViewModel = new(
                colorMeasurementService,
                fileService,
                dialogService,
                localizationService,
                settingsService,
                uploadService,
                dispatcher
            );

            MainWindow mainWindow = new()
                {
                DataContext = mainWindowViewModel
                };

            mainWindow.Closed += (s, args) => ( mainWindow.DataContext as IDisposable )?.Dispose();
            mainWindow.Show();
            }

        /// <summary>
        /// Handles language change event by reloading appropriate resource dictionary.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void LocalizationService_LanguageChanged(object sender, EventArgs e)
            {
            const string defaultResourcePath = "/Resources/StringResources.xaml";
            string cultureCode = LocalizationService.Instance.CurrentCulture.Name;

            string resourcePath = cultureCode switch
                {
                    "" => "/Resources/StringResources.xaml",
                    "zh-Hans" => "/Resources/StringResources.zh-Hans.xaml",
                    _ => defaultResourcePath
                    };

            try
                {
                var oldDictionaries = Application.Current.Resources.MergedDictionaries
                    .Where(d => d.Source != null &&
                               !d.Source.OriginalString.Contains("MaterialDesignThemes.Wpf") &&
                               !d.Source.OriginalString.Equals(new Uri(defaultResourcePath, UriKind.RelativeOrAbsolute).OriginalString, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var oldDict in oldDictionaries)
                    {
                    Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                    }

                ResourceDictionary newLanguageDictionary = new() { Source = new Uri(resourcePath, UriKind.RelativeOrAbsolute) };
                Application.Current.Resources.MergedDictionaries.Add(newLanguageDictionary);
                }
            catch (Exception ex)
                {
                Debug.WriteLine($"Failed to load resource dictionary '{resourcePath}': {ex.Message}");
                }
            }

        /// <summary>
        /// Performs cleanup of disposable services on application exit.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
            {
            ( colorMeasurementService as IDisposable )?.Dispose();
            ( fileService as IDisposable )?.Dispose();
            ( dialogService as IDisposable )?.Dispose();
            ( localizationService as IDisposable )?.Dispose();
            base.OnExit(e);
            }
        }
    }