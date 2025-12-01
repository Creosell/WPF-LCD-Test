namespace WPF_LCD_Test.Interfaces
{
    public interface IColorAnalyzer200 : IDisposable
    {
        IColorAnalyzer SingleCa { get; }
        void AutoConnect();
    }

    public interface IColorAnalyzer
    {
        IColorAnalyzerProbe SingleProbe { get; }
        IColorAnalyzerMemory Memory { get; }
        void CalZero();
        int SyncMode { get; set; }
        int AveragingMode { get; set; }
        void SetAnalogRange(float Range1, float Range2);
        int DisplayMode { get; set; }
        void Measure();
    }

    public interface IColorAnalyzerProbe
    {
        double sx { get; }
        double sy { get; }
        double Lv { get; }
        double T { get; }
    }

    public interface IColorAnalyzerMemory
    {
        int ChannelNO { get; set; }
    }
}