// В папке UnitTests/Mocks/TestViewModels.cs (или в любом удобном месте)

using MvvmHelpers; // Если BaseViewModel оттуда
using System;
using WPF_LCD_Test.Interfaces;

namespace WPF_LCD_Test.UnitTests.Mocks
{
    // Пример минимального MeasurementViewModel для тестирования
    public class MockMeasurementViewModel : BaseViewModel, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public IColorMeasurementService ColorMeasurementService { get; }
        public IFileService FileService { get; }
        public IDialogService DialogService { get; }
        public ILocalizationService LocalizationService { get; }

        public MockMeasurementViewModel(
            IColorMeasurementService colorMeasurementService,
            IFileService fileService,
            IDialogService dialogService,
            ILocalizationService localizationService)
        {
            ColorMeasurementService = colorMeasurementService;
            FileService = fileService;
            DialogService = dialogService;
            LocalizationService = localizationService;
        }

        public void Dispose()
        {
            IsDisposed = true;
            Console.WriteLine("MockMeasurementViewModel Disposed");
        }
    }

    // Пример минимального SettingsViewModel для тестирования
    public class MockSettingsViewModel : BaseViewModel, IDisposable
    {
        public bool IsDisposed { get; private set; }
        public ISettingsService SettingsService { get; }
        public ILocalizationService LocalizationService { get; }

        public MockSettingsViewModel(ISettingsService settingsService, ILocalizationService localizationService)
        {
            SettingsService = settingsService;
            LocalizationService = localizationService;
        }

        public void Dispose()
        {
            IsDisposed = true;
            Console.WriteLine("MockSettingsViewModel Disposed");
        }
    }
}