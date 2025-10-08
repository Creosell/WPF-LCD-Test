using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.ViewModels
{
    // ViewModel для страницы настроек приложения
    public class SettingsViewModel : BaseViewModel, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private string _devicePort;
        public string DevicePort
        {
            get => _devicePort;
            set => SetProperty(ref _devicePort, value);
        }
        private bool _autoConnectEnabled;
        public bool AutoConnectEnabled
        {
            get => _autoConnectEnabled;
            set => SetProperty(ref _autoConnectEnabled, value);
        }
        private string _languageCultureCode;
        public string LanguageCultureCode
        {
            get => _languageCultureCode;
            set => SetProperty(ref _languageCultureCode, value);
        }
        public class LanguageOption
        {
            public string DisplayName { get; set; }
            public string CultureCode { get; set; }
        }
        public ObservableCollection<LanguageOption> AvailableLanguages { get; } = [];
        private LanguageOption _selectedLanguage;
        public LanguageOption SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    if (_selectedLanguage != null)
                    {
                        LanguageCultureCode = _selectedLanguage.CultureCode;
                        try
                        {
                            _localizationService.SetLanguage(LanguageCultureCode);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"SettingsViewModel Ошибка при смене языка: {ex.Message}");
                        }
                        finally
                        {
                            SaveLanguageToSettings();
                        }
                    }
                    else
                    {
                        LanguageCultureCode = "";
                    }
                }
            }
        }
        public ICommand SaveSettingsCommand { get; }
        public ICommand CancelSettingsCommand { get; }

        // Конструктор: инициализация сервисов, команд, языков и загрузка настроек
        public SettingsViewModel(ISettingsService settingsService, ILocalizationService localizationService) : base()
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            PopulateAvailableLanguages();
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings, CanExecuteSaveSettings);
            CancelSettingsCommand = new RelayCommand(ExecuteCancelSettings, CanExecuteCancelSettings);
            LoadSettings();
            _localizationService.LanguageChanged += LocalizationService_LanguageChanged;
        }

        // Заполняет коллекцию доступных языков
        private void PopulateAvailableLanguages()
        {
            AvailableLanguages.Add(new() { DisplayName = Resources.Resources.English, CultureCode = "" });
            AvailableLanguages.Add(new() { DisplayName = Resources.Resources.Chinese, CultureCode = "zh-Hans" });
        }

        // Обновляет отображаемые названия языков при смене языка
        private void LocalizationService_LanguageChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("SettingsViewModel: Получено событие LanguageChanged.");
        }

        // Загружает настройки в ViewModel
        private void LoadSettings()
        {
            try
            {
                AppSettings currentSettings = _settingsService.LoadSettings();
                _languageCultureCode = currentSettings.LanguageCultureCode;
                SelectedLanguage = AvailableLanguages.FirstOrDefault(lang => lang.CultureCode == _languageCultureCode);
                if (SelectedLanguage == null)
                {
                    SelectedLanguage = AvailableLanguages.FirstOrDefault(lang => lang.CultureCode == "");
                    if (SelectedLanguage == null && AvailableLanguages.Any())
                    {
                        SelectedLanguage = AvailableLanguages.First();
                    }
                }
                DevicePort = currentSettings.DevicePort;
                AutoConnectEnabled = currentSettings.AutoConnectEnabled;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsViewModel Ошибка: Не удалось загрузить настройки. Ошибка: {ex.Message}");
            }
        }

        // Сохраняет настройки
        private void SaveSettings(AppSettings settingsToSave)
        {
            try
            {
                _settingsService.SaveSettings(settingsToSave);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsViewModel Ошибка: Не удалось сохранить настройки. Ошибка: {ex.Message}");
            }
        }

        // Выполняет сохранение текущих настроек
        private void ExecuteSaveSettings(object parameter)
        {
            AppSettings settingsToSave = new()
            {
                LanguageCultureCode = LanguageCultureCode,
                DevicePort = DevicePort,
                AutoConnectEnabled = AutoConnectEnabled
            };
            SaveSettings(settingsToSave);
        }

        // Сохраняет только язык
        private void SaveLanguageToSettings()
        {
            AppSettings settingsToSave = new()
            {
                LanguageCultureCode = LanguageCultureCode,
            };
            SaveSettings(settingsToSave);
        }

        private bool CanExecuteSaveSettings(object parameter) => true;
        private void ExecuteCancelSettings(object parameter) => LoadSettings();
        private bool CanExecuteCancelSettings(object parameter) => true;

        // Освобождает ресурсы и отписывается от событий
        public void Dispose()
        {
            _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            GC.SuppressFinalize(this);
            Debug.WriteLine("SettingsViewModel Dispose Called.");
        }
    }
}