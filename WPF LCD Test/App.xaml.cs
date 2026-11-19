using System.Diagnostics;
using System.IO.Abstractions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
#if DEBUG
using WPF_LCD_Test.DebugTools;
#endif
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
            AddService<ISettingsService, SettingsService>(services);
            AddService<ILocalizationService, LocalizationService>(services);
            services.AddSingleton<ISystemThemeProvider, WindowsSystemThemeProvider>();
            AddService<IThemeApplier, WpfThemeApplier>(services);
            AddService<IThemeService, ThemeService>(services);
            AddService<IMeasurementStatusService, MeasurementStatusService>(services);
            AddService<IAnalyzerRegistryService, AnalyzerRegistryService>(services);

            // Application services
            AddService<IColorMeasurementService, ColorMeasurementService>(services);
            AddService<IFileService, FileService>(services);
            AddService<IDialogService, DialogService>(services);
            AddService<IUploadService, UploadService>(services);
            AddService<ISpecEvaluationService, SpecEvaluationService>(services);

            // ViewModels
            AddViewModel<MainWindowViewModel>(services);
            AddViewModel<MeasurementViewModel>(services);
            AddViewModel<SettingsViewModel>(services);

            _serviceProvider = services.BuildServiceProvider();
            }

        /// <summary>
        /// Registers a singleton service. In the Debug configuration every call of the service is traced.
        /// </summary>
        private static void AddService<TService, TImplementation>(IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
            {
#if DEBUG
            services.AddSingleton<TService>(provider => TracingProxy.Wrap<TService>(ActivatorUtilities.CreateInstance<TImplementation>(provider)));
#else
            services.AddSingleton<TService, TImplementation>();
#endif
            }

        /// <summary>
        /// Registers a transient view model. In the Debug configuration every property change of the view model is traced.
        /// </summary>
        private static void AddViewModel<TViewModel>(IServiceCollection services)
            where TViewModel : class, System.ComponentModel.INotifyPropertyChanged
            {
#if DEBUG
            services.AddTransient<TViewModel>(provider => ViewModelTracer.Watch(ActivatorUtilities.CreateInstance<TViewModel>(provider)));
#else
            services.AddTransient<TViewModel>();
#endif
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
#if DEBUG
            DebugTrace.Start();
            UiEventTracer.Install();
            DebugTrace.Write("app", $"Screen Checker {typeof(App).Assembly.GetName().Version}, {Environment.OSVersion}, {Environment.ProcessorCount} cores, base directory {AppContext.BaseDirectory}");
#endif
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
                               d.Source.OriginalString.Contains("/Resources/StringResources", StringComparison.OrdinalIgnoreCase) &&
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