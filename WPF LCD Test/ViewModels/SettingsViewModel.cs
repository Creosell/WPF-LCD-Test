using MvvmHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
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

        public class SyncModeOption
            {
            public SyncMode Mode { get; set; }
            public string Name { get; set; }
            }
        private IReadOnlyList<SyncModeOption> _availableSyncModes = [];
        public IReadOnlyList<SyncModeOption> AvailableSyncModes
            {
            get => _availableSyncModes;
            private set => SetProperty(ref _availableSyncModes, value);
            }
        private SyncMode _syncMode;
        public SyncMode SyncMode
            {
            get => _syncMode;
            set
                {
                if (SetProperty(ref _syncMode, value))
                    {
                    OnPropertyChanged(nameof(SelectedSyncMode));
                    OnPropertyChanged(nameof(IsSyncFrequencyVisible));
                    OnPropertyChanged(nameof(IsIntegrationTimeVisible));
                    }
                }
            }

        /// <summary>
        /// Selection binding for the sync mode list; the null the list pushes while its items are replaced is ignored.
        /// </summary>
        public SyncMode? SelectedSyncMode
            {
            get => SyncMode;
            set
                {
                if (value.HasValue) SyncMode = value.Value;
                }
            }
        public bool IsSyncFrequencyVisible => SyncMode == SyncMode.Internal;
        public bool IsIntegrationTimeVisible => SyncMode == SyncMode.Manual;
        private string _syncFrequency = FormatValue(AnalyzerCapabilities.DefaultFrequency);
        public string SyncFrequency
            {
            get => _syncFrequency;
            set
                {
                if (value.Length < 8)
                    {
                    SetProperty(ref _syncFrequency, value);
                    }
                }
            }
        private string _integrationTime = FormatValue(AnalyzerCapabilities.DefaultIntegrationTime);
        public string IntegrationTime
            {
            get => _integrationTime;
            set
                {
                if (value.Length < 8)
                    {
                    SetProperty(ref _integrationTime, value);
                    }
                }
            }
        public string SyncFrequencyHint => FormatRange(AnalyzerCapabilities.GetFrequencyRange(_colorMeasurementService.Model), "Hz");
        public string IntegrationTimeHint => FormatRange(AnalyzerCapabilities.GetIntegrationTimeRange(), "ms");
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
        public ICommand ResetToDefaultCommand { get; }

        // Конструктор: инициализация сервисов, команд, языков и загрузка настроек
        public SettingsViewModel(ISettingsService settingsService, IDialogService dialogService, IColorMeasurementService colorMeasurementService, ILocalizationService localizationService) : base()
            {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _colorMeasurementService = colorMeasurementService ?? throw new ArgumentNullException(nameof(colorMeasurementService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            PopulateAvailableLanguages();
            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings, CanExecuteSaveSettings);
            ResetToDefaultCommand = new RelayCommand(ExecuteResetToDefault);
            LoadSettings();
            _localizationService.LanguageChanged += LocalizationService_LanguageChanged;
            _colorMeasurementService.CurrentChannelChanged += ColorMeasurementService_CurrentChannelChanged;
            _colorMeasurementService.ConnectionStatusChanged += ColorMeasurementService_ConnectionStatusChanged;
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

        private void ColorMeasurementService_ConnectionStatusChanged(object sender, bool e) => RefreshSyncOptions();

        // Ограничивает список режимов синхронизации возможностями подключённого прибора
        private void RefreshSyncOptions()
            {
            var modes = AnalyzerCapabilities.GetSyncModes(_colorMeasurementService.Model);
            if (!AvailableSyncModes.Select(option => option.Mode).SequenceEqual(modes))
                {
                AvailableSyncModes = modes.Select(mode => new SyncModeOption { Mode = mode, Name = mode.ToDisplayName() }).ToList();
                }
            if (modes.All(mode => mode != SyncMode))
                {
                SyncMode = SyncMode.Universal;
                }
            OnPropertyChanged(nameof(SelectedSyncMode));
            OnPropertyChanged(nameof(SyncFrequencyHint));
            OnPropertyChanged(nameof(IntegrationTimeHint));
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
                MeasurementSpeed = currentSettings.MeasurementSpeed;
                SyncMode = currentSettings.SyncMode;
                SyncFrequency = FormatValue(currentSettings.SyncFrequency ?? AnalyzerCapabilities.DefaultFrequency);
                IntegrationTime = FormatValue(currentSettings.IntegrationTime ?? AnalyzerCapabilities.DefaultIntegrationTime);
                RefreshSyncOptions();
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

            var saved = _settingsService.LoadSettings();
            AppSettings settingsToSave = new()
                {
                LanguageCultureCode = LanguageCultureCode,
                ColorAnalyzerChannel = ColorAnalyzerChannel,
                MeasurementSpeed = MeasurementSpeed,
                SyncMode = SyncMode,
                SyncFrequency = IsSyncFrequencyVisible
                    ? GetRangedValue(SyncFrequency, AnalyzerCapabilities.GetFrequencyRange(_colorMeasurementService.Model), saved.SyncFrequency, AnalyzerCapabilities.DefaultFrequency, "Hz", value => SyncFrequency = value)
                    : saved.SyncFrequency,
                IntegrationTime = IsIntegrationTimeVisible
                    ? GetRangedValue(IntegrationTime, AnalyzerCapabilities.GetIntegrationTimeRange(), saved.IntegrationTime, AnalyzerCapabilities.DefaultIntegrationTime, "ms", value => IntegrationTime = value)
                    : saved.IntegrationTime
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

        // Проверяет значение параметра синхронизации; при ошибке возвращает сохранённое (или значение по умолчанию)
        private double GetRangedValue(string text, (double Min, double Max) range, double? saved, double defaultValue, string unit, Action<string> setText)
            {
            if (double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                && result >= range.Min && result <= range.Max)
                {
                return result;
                }

            _dialogService.ShowMessage($"Allowed range: {FormatRange(range, unit)}", Err);
            result = saved is double previous && previous >= range.Min && previous <= range.Max ? previous : defaultValue;
            setText(FormatValue(result));
            return result;
            }

        private static string FormatValue(double value) => value.ToString(CultureInfo.InvariantCulture);

        private static string FormatRange((double Min, double Max) range, string unit) =>
            $"{FormatValue(range.Min)}-{FormatValue(range.Max)} {unit}";

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

        // Возвращает режимы измерения к значениям по умолчанию (канал и язык не меняются); сохраняется по Save
        private void ExecuteResetToDefault(object parameter)
            {
            var defaults = new AppSettings();
            MeasurementSpeed = defaults.MeasurementSpeed;
            SyncMode = defaults.SyncMode;
            SyncFrequency = FormatValue(AnalyzerCapabilities.DefaultFrequency);
            IntegrationTime = FormatValue(AnalyzerCapabilities.DefaultIntegrationTime);
            }

        // Освобождает ресурсы и отписывается от событий
        public void Dispose()
            {
            _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            _colorMeasurementService.CurrentChannelChanged -= ColorMeasurementService_CurrentChannelChanged;
            _colorMeasurementService.ConnectionStatusChanged -= ColorMeasurementService_ConnectionStatusChanged;
            GC.SuppressFinalize(this);
            Debug.WriteLine("SettingsViewModel Dispose Called.");
            }
        }
    }