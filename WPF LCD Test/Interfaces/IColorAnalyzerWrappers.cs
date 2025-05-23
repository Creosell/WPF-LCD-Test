// В папке Interfaces
// Файл IColorAnalyzerWrappers.cs

using System;
using System.Threading.Tasks;

namespace WPF_LCD_Test.Interfaces
{
    /// <summary>
    /// Интерфейс для обертки объекта Ca200 из библиотеки CA200SRVRLib.
    /// Предоставляет методы и свойства для управления главным контроллером анализатора цвета.
    /// </summary>
    public interface IColorAnalyzer200
    {
        // Свойство для доступа к обернутому Ca-объекту
        IColorAnalyzer SingleCa { get; }

        // Методы, соответствующие Ca200-объекту
        void AutoConnect();
        // Добавьте другие методы Ca200, которые вы используете (например, Close)
    }

    /// <summary>
    /// Интерфейс для обертки объекта Ca (SingleCa) из библиотеки CA200SRVRLib.
    /// Предоставляет методы и свойства для управления одним анализатором цвета.
    /// </summary>
    public interface IColorAnalyzer
    {
        // Свойства, соответствующие Ca-объекту
        int SyncMode { get; set; }
        int AveragingMode { get; set; }
        int DisplayMode { get; set; }

        // Добавим свойство для доступа к Probe, если это необходимо
        // Пример: если у Ca.SingleProbe есть свойства sx, sy, Lv, T
        IColorAnalyzerProbe SingleProbe { get; }

        // Методы, соответствующие Ca-объекту
        void CalZero();
        void Measure();
        void SetAnalogRange(float displayRange, float measureRange);

        // Управление памятью, если есть
        IColorAnalyzerMemory Memory { get; }
    }

    /// <summary>
    /// Интерфейс для обертки объекта Probe (SingleProbe) внутри Ca.
    /// </summary>
    public interface IColorAnalyzerProbe
    {
        double sx { get; }
        double sy { get; }
        double Lv { get; }
        double T { get; }
        // Добавьте другие свойства, которые вам нужны из Probe
    }

    /// <summary>
    /// Интерфейс для обертки объекта Memory (Memory) внутри Ca.
    /// </summary>
    public interface IColorAnalyzerMemory
    {
        int ChannelNO { get; set; }
        // Добавьте другие свойства/методы, если они используются (например, Load, Save)
    }

    
}