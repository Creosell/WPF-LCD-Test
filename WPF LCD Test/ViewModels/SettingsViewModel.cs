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
        private MeasurementSpeed _measurementSpeed;
        public MeasurementSpeed MeasurementSpeed
            {
            get => _measurementSpeed;
            set => SetProperty(ref _measurementSpeed, value);
            }
        public IReadOnlyList<MeasurementSpeed> AvailableMeasurementSpeeds { get; } = Enum.GetValues<MeasurementSpeed>();

        /// <summary>
        /// Synchronization modes the operator can choose from.
        /// </summary>
        public enum SyncModeOption { Universal, Internal }
        public IReadOnlyList<SyncModeOption> AvailableSyncModes { get; } = Enum.GetValues<SyncModeOption>();
        private SyncModeOption _syncMode;
        public SyncModeOption SyncMode
            {
            get => _syncMode;
            set
                {
                if (SetProperty(ref _syncMode, value)) OnPropertyChanged(nameof(IsSyncFrequencyEnabled));
                }
            }
        public bool IsSyncFrequencyEnabled => SyncMode == SyncModeOption.Internal;
        private string _syncFrequency = DefaultSyncFrequency.ToString();
        public string SyncFrequency
            {
            get => _syncFrequency;
            set
                {
                if (value.Length < 4)
                    {
                    SetProperty(ref _syncFrequency, value);
                    }
                }
            }
        private const int DefaultSyncFrequency = 60;
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
                MeasurementSpeed = currentSettings.MeasurementSpeed;
                SyncMode = currentSettings.SyncFrequency.HasValue ? SyncModeOption.Internal : SyncModeOption.Universal;
                SyncFrequency = (currentSettings.SyncFrequency ?? DefaultSyncFrequency).ToString();
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
                AutoConnectEnabled = AutoConnectEnabled,
                MeasurementSpeed = MeasurementSpeed,
                SyncFrequency = SyncMode == SyncModeOption.Internal ? GetSyncFrequency(SyncFrequency) : null
                };
            SaveSettings(settingsToSave);
            }

        // Сохраняет только язык
        private void SaveLanguageToSettings()
            {
            try
                {
                _settingsService.UpdateLanguage(LanguageCultureCode);
                }
            catch (Exception ex)
                {
                Debug.WriteLine($"SettingsViewModel Ошибка: Не удалось сохранить язык. Ошибка: {ex.Message}");
                }
            }

        // Проверяет частоту внутренней синхронизации; при ошибке возвращает сохранённое значение
        private int GetSyncFrequency(string frequency)
            {
            const int min = AppConstants.ColorAnalyzer.MinSyncFrequency;
            const int max = AppConstants.ColorAnalyzer.MaxSyncFrequency;
            if (int.TryParse(frequency, out int result) && result >= min && result <= max)
                {
                return result;
                }

            _dialogService.ShowMessage($"Sync frequency range: {min}-{max} Hz", Err);
            result = _settingsService.LoadSettings().SyncFrequency ?? DefaultSyncFrequency;
            SyncFrequency = result.ToString();
            return result;
            }

        private int GetChannel(string channel)
            {
            if (!int.TryParse(channel, out int result) || result < AppConstants.ColorAnalyzer.MinChannel || result > AppConstants.ColorAnalyzer.MaxChannel)
                {
                _dialogService.ShowMessage(
                    !int.TryParse(channel, out _)
                        ? "Invalid channel format"
                        : $"Available channels: {AppConstants.ColorAnalyzer.MinChannel}-{AppConstants.ColorAnalyzer.MaxChannel}",
                    Err);

                return int.TryParse(_settingsService.LoadSettings().ColorAnalyzerChannel, out int fallback)
                    ? fallback
                    : AppConstants.ColorAnalyzer.MinChannel;
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