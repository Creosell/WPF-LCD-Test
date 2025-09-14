// Файл: SettingsViewModel.cs
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
    public class SettingsViewModel : BaseViewModel, IDisposable
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IDialogService _dialogService;
        private int _measurementTime;
        private bool _autoConnectEnabled;
        private string _languageCultureCode;
        private LanguageOption _selectedLanguage;

        public ICommand ApplyMeasurementTimeCommand { get; private set; }

        public bool AutoConnectEnabled
        {
            get => _autoConnectEnabled;
            set => SetProperty(ref _autoConnectEnabled, value);
        }

        public int MeasurementTime
        {
            get => _measurementTime;
            set
            {
                if (value <= 0)
                {
                    OnPropertyChanged();
                    return;
                }
                SetProperty(ref _measurementTime, value);
            }
        }

        public string LanguageCultureCode
        {
            get => _languageCultureCode;
            set => SetProperty(ref _languageCultureCode, value);
        }

        public ObservableCollection<LanguageOption> AvailableLanguages { get; } = new ObservableCollection<LanguageOption>();

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
                        _localizationService.SetLanguage(LanguageCultureCode);
                        Debug.WriteLine($"SettingsViewModel: Язык сменен на '{LanguageCultureCode}'.");
                        SaveLanguageToSettings();
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

        public SettingsViewModel(ISettingsService settingsService, ILocalizationService localizationService) : base()
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _dialogService = App.Current is App app && app.MainWindow?.DataContext is IDialogService ds ? ds : null;

            PopulateAvailableLanguages();

            SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings);
            CancelSettingsCommand = new RelayCommand(ExecuteCancelSettings);
            ApplyMeasurementTimeCommand = new RelayCommand(ApplyMeasurementTime);

            LoadSettings();

            _localizationService.LanguageChanged += LocalizationService_LanguageChanged;
        }

        private void PopulateAvailableLanguages()
        {
            AvailableLanguages.Add(new LanguageOption { DisplayName = English, CultureCode = "" });
            AvailableLanguages.Add(new LanguageOption { DisplayName = Chinese, CultureCode = "zh-Hans" });
        }

        private void LocalizationService_LanguageChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("SettingsViewModel: Получено событие LanguageChanged.");
            PopulateAvailableLanguages();
        }

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

                MeasurementTime = currentSettings.MeasurementTime;
                AutoConnectEnabled = currentSettings.AutoConnectEnabled;

                Debug.WriteLine("SettingsViewModel: Настройки загружены.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsViewModel Ошибка: Не удалось загрузить настройки. Ошибка: {ex.Message}");
            }
        }

        private void ExecuteSaveSettings(object parameter)
        {
            AppSettings settingsToSave = new AppSettings
            {
                LanguageCultureCode = LanguageCultureCode,
                MeasurementTime = MeasurementTime,
                AutoConnectEnabled = AutoConnectEnabled
            };
            SaveSettings(settingsToSave);
        }

        private void SaveLanguageToSettings()
        {
            AppSettings settingsToSave = new AppSettings
            {
                LanguageCultureCode = LanguageCultureCode,
            };
            SaveSettings(settingsToSave);
        }

        private void ExecuteCancelSettings(object parameter)
        {
            LoadSettings();
        }

        private void SaveSettings(AppSettings settingsToSave)
        {
            try
            {
                _settingsService.SaveSettings(settingsToSave);
                Debug.WriteLine("SettingsViewModel: Настройки сохранены.");
                // Notify UI if MeasurementTime changed
                OnPropertyChanged(nameof(MeasurementTime));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SettingsViewModel Ошибка: Не удалось сохранить настройки. Ошибка: {ex.Message}");
            }
        }

        private void ApplyMeasurementTime(object parameter)
        {
            try
            {
                var enteredMeasurementTime = parameter as string;

                if (string.IsNullOrWhiteSpace(enteredMeasurementTime) || !int.TryParse(enteredMeasurementTime, out int measurementTime) || measurementTime <= 0)
                {
                    _dialogService?.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
                    return;
                }
                MeasurementTime = measurementTime;
            }
            catch
            {
                _dialogService?.ShowMessage($"{IncorrectMeasTimeFormat}", $"{Err}");
            }
        }

        public void Dispose()
        {
            _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
            GC.SuppressFinalize(this);
            Debug.WriteLine("SettingsViewModel Dispose Called.");
        }

        public class LanguageOption
        {
            public string DisplayName { get; set; }
            public string CultureCode { get; set; }
        }
    }
}