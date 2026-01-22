using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using static WPF_LCD_Test.Resources.Resources;

namespace WPF_LCD_Test.ViewModels
    {
    // ViewModel для страницы настроек приложения
    public class SettingsViewModel : BaseViewModel, IDisposable
        {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IColorMeasurementService _colorMeasurementService;
        private readonly IDialogService _dialogService;
        private string _colorAnalyzerChannel;
        private const int MinChannel = 0;
        private const int MaxChannel = 99;

        public string ColorAnalyzerChannel
            {
            get => _colorAnalyzerChannel;
            set
                {
                if (value.Length<3)
                    {
                    SetProperty(ref _colorAnalyzerChannel, value);
                    }
                }
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
        public SettingsViewModel(ISettingsService settingsService, IDialogService dialogService, IColorMeasurementService colorMeasurementService, ILocalizationService localizationService) : base()
            {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            PopulateAvailableLanguages();
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings, CanExecuteSaveSettings);
            CancelSettingsCommand = new RelayCommand(ExecuteCancelSettings, CanExecuteCancelSettings);
            LoadSettings();
            _localizationService.LanguageChanged += LocalizationService_LanguageChanged;
            _colorMeasurementService.CurrentChannelChanged += ColorMeasurementService_CurrentChannelChanged;
            }

        // Заполняет коллекцию доступных языков
        private void PopulateAvailableLanguages()
            {
            AvailableLanguages.Add(new() { DisplayName = English, CultureCode = "" });
            AvailableLanguages.Add(new() { DisplayName = Chinese, CultureCode = "zh-Hans" });
            }

        // Обновляет отображаемые названия языков при смене языка
        private void LocalizationService_LanguageChanged(object sender, EventArgs e)
            {
            Debug.WriteLine("SettingsViewModel: Получено событие LanguageChanged.");
            }

        private void ColorMeasurementService_CurrentChannelChanged(object sender, int e)
            {
            ColorAnalyzerChannel = _colorMeasurementService.CurrentChannel.ToString();
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
                ColorAnalyzerChannel = currentSettings.ColorAnalyzerChannel;
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

            ExecuteChangeChannel(ColorAnalyzerChannel);

            AppSettings settingsToSave = new()
                {
                LanguageCultureCode = LanguageCultureCode,
                ColorAnalyzerChannel = ColorAnalyzerChannel,
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

        private int GetChannel(string channel)
            {
            if (!int.TryParse(channel, out int result) || result < MinChannel || result > MaxChannel)
                {
                _dialogService.ShowMessage(
                    !int.TryParse(channel, out _)
                        ? "Invalid channel format"
                        : $"Available channels: {MinChannel}-{MaxChannel}",
                    Err);

                return int.TryParse(_settingsService.LoadSettings().ColorAnalyzerChannel, out int fallback)
                    ? fallback
                    : MinChannel;
                }

            return result;
            }

        private void ExecuteChangeChannel(string channel)
            {

            int channelInt = GetChannel(channel);

            ColorAnalyzerChannel = channelInt.ToString();

            _colorMeasurementService.CurrentChannel = channelInt;
            }

        private bool CanExecuteSaveSettings(object parameter) => true;
        private void ExecuteCancelSettings(object parameter) => LoadSettings();
        private bool CanExecuteCancelSettings(object parameter) => true;

        // Освобождает ресурсы и отписывается от событий
        public void Dispose()
            {
            _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            _colorMeasurementService.CurrentChannelChanged -= ColorMeasurementService_CurrentChannelChanged;
            GC.SuppressFinalize(this);
            Debug.WriteLine("SettingsViewModel Dispose Called.");
            }
        }
    }