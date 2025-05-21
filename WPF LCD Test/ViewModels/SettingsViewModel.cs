// В папке ViewModels
// Файл SettingsViewModel.cs

using System;
using System.Collections.ObjectModel; // Для ObservableCollection
using System.Globalization; // Для CultureInfo
using System.Linq; // Для Linq (FirstOrDefault)
using System.Windows.Input;
using WPF_LCD_Test.ViewModels;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;
using System.Diagnostics;
using MvvmHelpers;
using WPF_LCD_Test.Commands; // Для Debug.WriteLine


    // Определите этот вспомогательный класс где-то в вашем проекте,
    // например, в папке Models или ViewModels, или в отдельном файле Helpers.
    // Он нужен для элементов ComboBox.
   


    namespace WPF_LCD_Test.ViewModels
    {
        public class SettingsViewModel : BaseViewModel, IDisposable
        {
            private readonly ISettingsService _settingsService;
            private readonly ILocalizationService _localizationService;
            // private readonly INavigationService _navigationService; // Если нужен для навигации после сохранения

            // --- СВОЙСТВА НАСТРОЕК (уже были) ---
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
            public string LanguageCultureCode // Храним выбранный код культуры
            {
                get => _languageCultureCode;
                set => SetProperty(ref _languageCultureCode, value); // Используем SetProperty для уведомления
            }

        public class LanguageOption
        {
            public string DisplayName { get; set; } // Отображаемое имя языка (например, "English")
            public string CultureCode { get; set; } // Код культуры (например, "en")
        }
        // TODO: Добавьте другие свойства для всех ваших настроек

        // --- СВОЙСТВА ДЛЯ COMBOBOX ЯЗЫКОВ ---
        /// <summary>
        /// Коллекция доступных языков для ComboBox.
        /// </summary>
        public ObservableCollection<LanguageOption> AvailableLanguages { get; } = [];

            private LanguageOption _selectedLanguage;
            /// <summary>
            /// Выбранный язык в ComboBox.
            /// </summary>
            public LanguageOption SelectedLanguage
            {
                get => _selectedLanguage;
                set
                {
                    if (SetProperty(ref _selectedLanguage, value))
                    {
                        // При изменении выбранного элемента в ComboBox:
                        if (_selectedLanguage != null)
                        {
                            // 1. Обновляем свойство LanguageCultureCode в ViewModel
                            LanguageCultureCode = _selectedLanguage.CultureCode;

                            // 2. !!! СРАЗУ ЖЕ ВЫЗЫВАЕМ СЕРВИС ЛОКАЛИЗАЦИИ ДЛЯ СМЕНЫ ЯЗЫКА !!!
                            try
                            {
                                _localizationService.SetLanguage(LanguageCultureCode);
                                Debug.WriteLine($"SettingsViewModel: Язык сменен на '{LanguageCultureCode}' при выборе в ComboBox.");
                                // После смены языка, событие LanguageChanged в LocalizationService вызовется,
                                // и наш обработчик LocalizationService_LanguageChanged обновит AvailableLanguages
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"SettingsViewModel Ошибка при смене языка: {ex.Message}");
                                // TODO: Обработка ошибок смены языка
                            }
                            finally
                            {
                                SaveLanguageToSettings();
                            }
                        }
                        else
                        {
                            // Если ComboBox сброшен (null), можно установить язык по умолчанию
                            // _localizationService.SetLanguage("en"); // Установите ваш язык по умолчанию
                            LanguageCultureCode = "en"; // Просто обновляем свойство
                        }

                        //UpdateCommandsCanExecute();
                    }
                }
            }


            // --- КОМАНДЫ (уже были) ---
            public ICommand SaveSettingsCommand { get; }
            public ICommand CancelSettingsCommand { get; }


            // --- КОНСТРУКТОР ---
            public SettingsViewModel(ISettingsService settingsService, ILocalizationService localizationService /*, INavigationService navigationService*/) : base()
            {
                _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
                _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
                // _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

                // !!! Инициализация коллекции доступных языков !!!
                PopulateAvailableLanguages();

                // Инициализация команд
                SaveSettingsCommand = new RelayCommand(ExecuteSaveSettings, CanExecuteSaveSettings);
                CancelSettingsCommand = new RelayCommand(ExecuteCancelSettings, CanExecuteCancelSettings);

                // Загрузка текущих настроек при создании ViewModel
                LoadSettings();

                // TODO: Подписка на события сервисов, если нужно
                _localizationService.LanguageChanged += LocalizationService_LanguageChanged;
            }

            // Simplified 'new' expressions in PopulateAvailableLanguages method
            private void PopulateAvailableLanguages()
            {
                AvailableLanguages.Add(new() { DisplayName = Resources.Resources.English, CultureCode = "en" });
                AvailableLanguages.Add(new() { DisplayName = Resources.Resources.Chinese, CultureCode = "zh-Hans" });

                // TODO: Add other languages if supported
            }

            private void LocalizationService_LanguageChanged(object sender, EventArgs e)
            {
                Debug.WriteLine("SettingsViewModel: Получено событие LanguageChanged.");
                // При смене языка, нужно обновить отображаемые названия языков в ComboBox
                // Перезаполняем коллекцию с новыми локализованными названиями
                // SelectedLanguage setter уже обновит LanguageCultureCode и установит язык UI.
                // Здесь главное - обновить сами элементы в коллекции.

                // Возможно, нужно обновить другие локализуемые строки в этом ViewModel,
                // если у вас есть свойства типа string, которые должны менять значение при смене языка
                // Например: SettingPageTitle = Resources.Resources.SettingsTitle; OnPropertyChanged(nameof(SettingPageTitle));
            }


            // --- МЕТОДЫ ЗАГРУЗКИ/СОХРАНЕНИЯ НАСТРОЕК В VIEWMODEL ---

            private void LoadSettings()
            {
                try
                {
                    AppSettings currentSettings = _settingsService.LoadSettings();

                    // !!! Обновляем свойства ViewModel значениями из загруженных настроек !!!
                    // Сначала обновляем LanguageCultureCode (он будет использоваться в SelectedLanguage setter)
                    _languageCultureCode = currentSettings.LanguageCultureCode; // Устанавливаем поле напрямую, чтобы избежать триггера SelectedLanguage setter рекурсивно

                    // Находим LanguageOption, соответствующий загруженному коду культуры, и устанавливаем SelectedLanguage
                    SelectedLanguage = AvailableLanguages.FirstOrDefault(lang => lang.CultureCode == _languageCultureCode);

                    // Если загруженный язык не найден в AvailableLanguages (например, если файл настроек был изменен вручную),
                    // SelectedLanguage будет null. Можно выбрать язык по умолчанию в этом случае.
                    if (SelectedLanguage == null)
                    {
                        SelectedLanguage = AvailableLanguages.FirstOrDefault(lang => lang.CultureCode == "en"); // Выберите ваш язык по умолчанию
                        if (SelectedLanguage == null && AvailableLanguages.Any()) // Если даже язык по умолчанию не найден, выберите первый доступный
                        {
                            SelectedLanguage = AvailableLanguages.First();
                        }
                    }

                    // TODO: Обновите другие свойства ViewModel из currentSettings
                    DevicePort = currentSettings.DevicePort;
                    AutoConnectEnabled = currentSettings.AutoConnectEnabled;


                    Debug.WriteLine("SettingsViewModel: Настройки загружены в ViewModel.");
                    //UpdateCommandsCanExecute();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SettingsViewModel Ошибка: Не удалось загрузить настройки. Ошибка: {ex.Message}");
                }
            }

            private void SaveSettings(AppSettings settingsToSave)
            {
                try
                {
                    _settingsService.SaveSettings(settingsToSave);
                    Debug.WriteLine("SettingsViewModel: Настройки сохранены через сервис.");

                    // !!! После успешного сохранения настроек, ПРИМЕНЯЕМ выбранный язык !!!
                    // Это вызовет событие LanguageChanged в LocalizationService,
                    // которое App.xaml.cs обработает для подмены словарей ресурсов.
                    //LocalizationService.Instance.SetLanguage(settingsToSave.LanguageCultureCode);

                    //UpdateCommandsCanExecute(); // Обновляем доступность команд после сохранения

                    // TODO: Возможно, нужно вернуться на предыдущую страницу после сохранения
                    // _navigationService.NavigateBack();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"SettingsViewModel Ошибка: Не удалось сохранить настройки. Ошибка: {ex.Message}");
                }
            }


            // --- МЕТОДЫ ВЫПОЛНЕНИЯ КОМАНД ---

            private void ExecuteSaveSettings(object parameter)
            {
                // !!! Собираем текущие значения настроек из свойств ViewModel в объект AppSettings !!!
                AppSettings settingsToSave = new()
                {
                    LanguageCultureCode = LanguageCultureCode, // Берем код из свойства ViewModel
                                                               // TODO: Заполните остальные свойства из ViewModel
                    DevicePort = DevicePort,
                    AutoConnectEnabled = AutoConnectEnabled
                };

                // Вызываем вспомогательный метод сохранения
                SaveSettings(settingsToSave);
            }

            // Simplified 'new' expression in SaveLanguageToSettings method
            private void SaveLanguageToSettings()
            {
                // Save language changes
                AppSettings settingsToSave = new()
                {
                    LanguageCultureCode = LanguageCultureCode,
                };

                // Call helper method to save settings
                SaveSettings(settingsToSave);
            }

            private bool CanExecuteSaveSettings(object parameter)
            {
                // TODO: Логика доступности команды "Сохранить" (например, если настройки были изменены)
                return true; // Пока всегда доступно
            }

            private void ExecuteCancelSettings(object parameter)
            {
                // !!! Перезагружаем настройки из файла, отбрасывая несохраненные изменения !!!
                LoadSettings();

                // TODO: Возможно, нужно вернуться на предыдущую страницу после отмены
                // _navigationService.NavigateBack();
            }

            private bool CanExecuteCancelSettings(object parameter)
            {
                // TODO: Логика доступности команды "Отмена"
                return true; // Пока всегда доступно
            }

            // --- Метод ExecuteSwitchLanguage, который вы предоставили ---
            // Если вы применяете язык при сохранении, этот метод и соответствующая команда не нужны
            // Если вы хотите применять язык СРАЗУ при выборе в ComboBox, то логику из SaveSettings.SetLanguage()
            // нужно перенести сюда, и эта команда должна вызываться при изменении SelectedLanguage
            /*
             private void ExecuteSwitchLanguage(object parameter)
             {
                  // Этот метод вызывался бы командой
                  // Логику переключения языка теперь лучше делать при Сохранении настроек,
                  // чтобы язык применялся после подтверждения пользователем.
                  // Если нужно применять сразу, эта логика должна вызываться при изменении SelectedLanguage.

                 // if (!CanExecuteSwitchLanguage(parameter)) return; // CanExecute для этой команды, если она есть

                 // string? languageCode = parameter as string; // Получаем код языка из параметра команды
                 // if (string.IsNullOrWhiteSpace(languageCode))
                 // {
                 //     return;
                 // }

                 // try
                 // {
                 //     // Вызов сервиса локализации для смены языка
                 //     LocalizationService.Instance.SetLanguage(languageCode);

                 //     // TODO: Возможно, нужно обновить настройки в ViewModel,
                 //     // чтобы в ComboBox отобразился выбранный язык после смены языка UI,
                 //     // так как DisplayName в LanguageOption привязан к DynamicResource.
                 //     // PopulateAvailableLanguages(); // Перезаполнение коллекции с новыми локализованными именами

                 // }
                 // catch (Exception ex)
                 // {
                 //      // Используйте DialogService из ViewModel для оповещения
                 //     // _dialogService.ShowMessage($"Ошибка смены языка: {ex.Message}", "Ошибка");
                 // }
             }
             */


            // TODO: Добавьте вспомогательные методы (например, для отслеживания изменений)


            // --- РЕАЛИЗАЦИЯ IDisposable ---
            public void Dispose()
            {
                _localizationService.LanguageChanged -= LocalizationService_LanguageChanged;
                GC.SuppressFinalize(this);
                Debug.WriteLine("SettingsViewModel Dispose Called.");

                // TODO: Отписка от событий сервисов, если была подписка в конструкторе
            }
        }
    }
