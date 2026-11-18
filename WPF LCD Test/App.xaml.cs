using System.Diagnostics;
using System.IO.Abstractions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.Services;
using WPF_LCD_Test.ViewModels;
using WPF_LCD_Test.Views;
using WPF_LCD_Test.Wrappers;

namespace WPF_LCD_Test
    {
    public partial class App : Application
        {
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Configures dependency injection container with all application services.
        /// </summary>
        private void ConfigureServices()
            {
            var services = new ServiceCollection();

            // Infrastructure services
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IDispatcher, WpfDispatcher>();
            services.AddSingleton<IPathProvider>(new PathProvider());
            services.AddSingleton<IProcessRunner, ProcessRunner>();

            // Core services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<ILocalizationService, LocalizationService>();
            services.AddSingleton<ISystemThemeProvider, WindowsSystemThemeProvider>();
            services.AddSingleton<IThemeApplier, WpfThemeApplier>();
            services.AddSingleton<IThemeService, ThemeService>();
            services.AddSingleton<IMeasurementStatusService, MeasurementStatusService>();
            services.AddSingleton<IAnalyzerRegistryService, AnalyzerRegistryService>();

            // Application services
            services.AddSingleton<IColorMeasurementService, ColorMeasurementService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IUploadService, UploadService>();
            services.AddSingleton<ISpecEvaluationService, SpecEvaluationService>();

            // ViewModels
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<MeasurementViewModel>();
            services.AddTransient<SettingsViewModel>();

            _serviceProvider = services.BuildServiceProvider();
            }

        /// <summary>
        /// Removes the dotted keyboard focus rectangle from every element of the application. The value is set locally
        /// when an element loads, so it also wins over the focus styles of the Material Design templates.
        /// </summary>
        private static void HideFocusVisuals()
            {
            EventManager.RegisterClassHandler(
                typeof(FrameworkElement),
                FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) => ((FrameworkElement)sender).FocusVisualStyle = null));

            // Safety net for elements that were created or restyled after loading: cleared right before the focus lands
            EventManager.RegisterClassHandler(
                typeof(FrameworkElement),
                Keyboard.PreviewGotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler((_, e) =>
                    {
                    if (e.NewFocus is FrameworkElement focused) focused.FocusVisualStyle = null;
                    }));
            }

        /// <summary>
        /// Initializes application services, loads settings, configures localization, and displays main window.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Startup event arguments.</param>
        private void Application_Startup(object sender, StartupEventArgs e)
            {
            HideFocusVisuals();
            ConfigureServices();

            var settingsService = _serviceProvider.GetRequiredService<ISettingsService>();
            var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
            var colorMeasurementService = _serviceProvider.GetRequiredService<IColorMeasurementService>();

            AppSettings appSettings = settingsService.LoadSettings();

            _serviceProvider.GetRequiredService<IThemeService>().Initialize();

            localizationService.LanguageChanged += LocalizationService_LanguageChanged;
            localizationService.SetLanguage(appSettings.LanguageCultureCode);

            if (int.TryParse(appSettings.ColorAnalyzerChannel, out int channel))
                {
                colorMeasurementService.CurrentChannel = channel;
                }

            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

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
            var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
            string cultureCode = localizationService.CurrentCulture.Name;

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
            if (_serviceProvider is IDisposable disposable)
                {
                disposable.Dispose();
                }
            base.OnExit(e);
            }
        }
    }