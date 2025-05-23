// В папке Services/Wrappers
// Файл ColorAnalyzerWrappers.cs

using CA200SRVRLib; // Теперь здесь используем оригинальную библиотеку
using WPF_LCD_Test.Interfaces; // Импортируем наши интерфейсы
using System;
using System.Runtime.InteropServices; // Для Marshal.ReleaseComObject

namespace WPF_LCD_Test.Services.Wrappers
{
    /// <summary>
    /// Обёртка для объекта Ca200.
    /// Отвечает за создание и освобождение COM-объекта Ca200.
    /// </summary>
    public class ColorAnalyzer200Wrapper : IColorAnalyzer200, IDisposable
    {
        private Ca200? _objCa200;
        private IColorAnalyzer? _singleCaWrapper;
        private bool _disposed = false;

        public ColorAnalyzer200Wrapper()
        {
            // Создаем COM-объект при создании обертки
            _objCa200 = new Ca200();
        }

        public IColorAnalyzer SingleCa
        {
            get
            {
                if (_objCa200 == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzer200Wrapper), "CA200 object has been disposed.");
                }
                // Создаем обертку для SingleCa только при первом запросе
                // Это позволяет избежать создания обертки, если она не нужна
                _singleCaWrapper ??= new ColorAnalyzerWrapper(_objCa200.SingleCa);
                return _singleCaWrapper;
            }
        }

        public void AutoConnect()
        {
            if (_objCa200 == null)
            {
                throw new ObjectDisposedException(nameof(ColorAnalyzer200Wrapper), "CA200 object has been disposed.");
            }
            _objCa200.AutoConnect();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Освобождаем управляемые ресурсы, если таковые имеются.
                    // В данном случае их нет, так как _singleCaWrapper не требует явного Dispose,
                    // его внутренний COM-объект освобождается вместе с _objCa200.
                }

                // Освобождаем неуправляемые ресурсы (COM-объекты)
                if (_objCa200 != null)
                {
                    try
                    {
                        // Важно: Marshal.ReleaseComObject должен вызываться для объекта,
                        // который был создан в .NET (через new Ca200()).
                        Marshal.ReleaseComObject(_objCa200);
                    }
                    catch (Exception ex)
                    {
                        // Логирование ошибки при освобождении COM-объекта
                        Console.WriteLine($"Error releasing Ca200 COM object: {ex.Message}");
                    }
                    _objCa200 = null;
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ColorAnalyzer200Wrapper()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Обёртка для объекта Ca (SingleCa).
    /// </summary>
    public class ColorAnalyzerWrapper : IColorAnalyzer
    {
        private readonly Ca _objCa;
        private readonly IColorAnalyzerProbe _singleProbe;
        private readonly IColorAnalyzerMemory _memory;

        public ColorAnalyzerWrapper(Ca caInstance)
        {
            _objCa = caInstance ?? throw new ArgumentNullException(nameof(caInstance));
            _singleProbe = new ColorAnalyzerProbeWrapper(_objCa.SingleProbe);
            _memory = new ColorAnalyzerMemoryWrapper(_objCa.Memory);
        }

        public int SyncMode
        {
            get => (int) _objCa.SyncMode;
            set => _objCa.SyncMode = value;
        }

        public int AveragingMode
        {
            get => _objCa.AveragingMode;
            set => _objCa.AveragingMode = value;
        }

        public int DisplayMode
        {
            get => _objCa.DisplayMode;
            set => _objCa.DisplayMode = value;
        }

        public IColorAnalyzerProbe SingleProbe => _singleProbe;
        public IColorAnalyzerMemory Memory => _memory;

        public void CalZero()
        {
            _objCa.CalZero();
        }

        public void Measure()
        {
            _objCa.Measure();
        }

        public void SetAnalogRange(float displayRange, float measureRange)
        {
            _objCa.SetAnalogRange(displayRange, measureRange);
        }
    }

    /// <summary>
    /// Обёртка для объекта Probe (SingleProbe).
    /// </summary>
    public class ColorAnalyzerProbeWrapper : IColorAnalyzerProbe
    {
        private readonly Probe _probeInstance;

        public ColorAnalyzerProbeWrapper(Probe probeInstance)
        {
            _probeInstance = probeInstance ?? throw new ArgumentNullException(nameof(probeInstance));
        }

        public double sx => _probeInstance.sx;
        public double sy => _probeInstance.sy;
        public double Lv => _probeInstance.Lv;
        public double T => _probeInstance.T;
    }

    /// <summary>
    /// Обёртка для объекта Memory.
    /// </summary>
    public class ColorAnalyzerMemoryWrapper : IColorAnalyzerMemory
    {
        private readonly Memory _memoryInstance;

        public ColorAnalyzerMemoryWrapper(Memory memoryInstance)
        {
            _memoryInstance = memoryInstance ?? throw new ArgumentNullException(nameof(memoryInstance));
        }

        public int ChannelNO
        {
            get => _memoryInstance.ChannelNO;
            set => _memoryInstance.ChannelNO = value;
        }
    }
}