using System;

namespace WPF_LCD_Test.Interfaces
{
    // Интерфейс для корневого объекта CA200SRVRLib.Ca200
    public interface IColorAnalyzer200 : IDisposable
    {
        IColorAnalyzer SingleCa { get; }
        void AutoConnect();
        // Убрали void Disconnect(); так как он не освобождает порт как нужно
        // Добавьте сюда другие методы/свойства Ca200, если они используются
    }

    // Интерфейс для объекта CA200SRVRLib.Ca (получается через Ca200.SingleCa)
    public interface IColorAnalyzer
    {
        IColorAnalyzerProbe SingleProbe { get; }
        IColorAnalyzerMemory Memory { get; }

        void CalZero();
        int SyncMode { get; set; }
        int AveragingMode { get; set; }
        void SetAnalogRange(float Range1, float Range2);
        int DisplayMode { get; set;}
        void Measure();
        // Добавьте другие методы/свойства Ca, если они используются
    }

    // Интерфейс для объекта CA200SRVRLib.Probe (получается через Ca.SingleProbe)
    public interface IColorAnalyzerProbe
    {
        double sx { get; }
        double sy { get; }
        double Lv { get; }
        double T { get; }
        // Добавьте другие свойства Probe, если они используются
    }

    // Интерфейс для объекта CA200SRVRLib.Memory (получается через Ca.Memory)
    public interface IColorAnalyzerMemory
    {
        int ChannelNO { get; set; }
        // Добавьте другие свойства/методы Memory, если они используются
    }
}