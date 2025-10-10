using System;
using System.Threading.Tasks;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Interfaces
{
    public interface IFileService
    {
        void InitializeWorkingFolders();
        string BaseFolderPath { get; }
        string WorkFolderName { get; }
        Task<bool> SaveDeviceDataToJsonAsync(DeviceUnderTest device);
        Task<bool> SaveMeasurementToCsvAsync(string measurementCsvString, string measurementLocationName, string serialNumber);
        event EventHandler<string> StatusMessage;
        event EventHandler<bool> SaveOperationCompleted;
        bool RunExternalProgram(string executableName);
    }
}