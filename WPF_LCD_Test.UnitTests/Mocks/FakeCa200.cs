// В проекте WPF_LCD_Test.UnitTests
// Папка Mocks (или TestStubs)
// Файл FakeCa200.cs

using System.Runtime.InteropServices; // Для COMException

// Имитация интерфейсов из CA200SRVRLib
// Обратите внимание: мы не импортируем CA200SRVRLib здесь,
// а просто создаем классы с теми же именами и необходимыми членами.
// Это позволяет избежать прямой зависимости тестового проекта от COM-библиотеки.

namespace WPF_LCD_Test.UnitTests.Mocks
{
    // Fake для Ca200
    public class FakeCa200
    {
        public FakeCa _singleCa;
        public Action AutoConnectAction { get; set; } // Делегат для имитации AutoConnect

        public FakeCa200()
        {
            _singleCa = new FakeCa();
        }

        public FakeCa SingleCa => _singleCa;

        public void AutoConnect()
        {
            AutoConnectAction?.Invoke();
        }
    }

    // Fake для Ca
    public class FakeCa
    {
        public FakeProbe SingleProbe { get; set; } = new FakeProbe();
        public Action CalZeroAction { get; set; } // Делегат для имитации CalZero
        public Action MeasureAction { get; set; } // Делегат для имитации Measure

        public int SyncMode { get; set; }
        public int AveragingMode { get; set; }
        public int DisplayMode { get; set; }
        public FakeMemory Memory { get; set; } = new FakeMemory();

        public void CalZero()
        {
            CalZeroAction?.Invoke();
        }

        public void Measure()
        {
            MeasureAction?.Invoke();
        }

        public void SetAnalogRange(float RangeVal, float RangeLv)
        {
            // Не делаем ничего, просто имитируем вызов
        }
    }

    // Fake для SingleProbe (внутри Ca)
    public class FakeProbe
    {
        public double sx { get; set; }
        public double sy { get; set; }
        public double Lv { get; set; }
        public double T { get; set; }
    }

    // Fake для Memory (внутри Ca)
    public class FakeMemory
    {
        public int ChannelNO { get; set; }
    }
}