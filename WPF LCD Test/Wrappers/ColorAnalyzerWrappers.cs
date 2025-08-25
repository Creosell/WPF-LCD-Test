// В папке Wrappers
// Файл ColorAnalyzerWrapper.cs
using CA200SRVRLib; // Прямая ссылка на COM-библиотеку
using System.Runtime.InteropServices;
using WPF_LCD_Test.Interfaces; // Наши объединенные интерфейсы

namespace WPF_LCD_Test.Wrappers
{
    /// <summary>
    /// Обёртка для объекта CA200SRVRLib.Memory, реализующая IColorAnalyzerMemory.
    /// </summary>
    public class ColorAnalyzerMemoryWrapper : IColorAnalyzerMemory
    {
        private Memory? _memoryInstance; // Реальный COM-объект CA200SRVRLib.Memory

        public ColorAnalyzerMemoryWrapper(Memory memoryInstance)
        {
            _memoryInstance = memoryInstance ?? throw new ArgumentNullException(nameof(memoryInstance));
        }

        // Реализация свойств из IColorAnalyzerMemory
        public int ChannelNO
        {
            get
            {
                if (_memoryInstance == null) throw new ObjectDisposedException(nameof(ColorAnalyzerMemoryWrapper));
                return _memoryInstance.ChannelNO;
            }
            set
            {
                if (_memoryInstance == null) throw new ObjectDisposedException(nameof(ColorAnalyzerMemoryWrapper));
                _memoryInstance.ChannelNO = value;
            }
        }
    }

    /// <summary>
    /// Обёртка для объекта CA200SRVRLib.Probe, реализующая IColorAnalyzerProbe.
    /// </summary>
    public class ColorAnalyzerProbeWrapper : IColorAnalyzerProbe
    {
        private Probe? _probeInstance; // Реальный COM-объект CA200SRVRLib.Probe

        public ColorAnalyzerProbeWrapper(Probe probeInstance)
        {
            _probeInstance = probeInstance ?? throw new ArgumentNullException(nameof(probeInstance));
        }

        // Реализация свойств из IColorAnalyzerProbe
        public double sx
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(ColorAnalyzerProbeWrapper));
                return _probeInstance.sx;
            }
        }

        public double sy
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(ColorAnalyzerProbeWrapper));
                return _probeInstance.sy;
            }
        }

        public double Lv
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(ColorAnalyzerProbeWrapper));
                return _probeInstance.Lv;
            }
        }

        public double T
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(ColorAnalyzerProbeWrapper));
                return _probeInstance.T;
            }
        }
    }

    /// <summary>
    /// Обёртка для объекта CA200SRVRLib.Ca, реализующая IColorAnalyzer.
    /// Этот объект получается из Ca200.SingleCa, поэтому он не создает Ca,
    /// а лишь оборачивает существующий, предоставленный Ca200.
    /// Он также отвечает за освобождение своих дочерних COM-объектов (Probe, Memory)
    /// путем обнуления ссылок на их обертки.
    /// </summary>
    public class ColorAnalyzerWrapper : IColorAnalyzer
    {
        private Ca? _caInstance; // Реальный COM-объект CA200SRVRLib.Ca
        private ColorAnalyzerProbeWrapper? _singleProbeWrapper; // Обёртка для IColorAnalyzerProbe
        private ColorAnalyzerMemoryWrapper? _memoryWrapper; // Обёртка для IColorAnalyzerMemory

        public ColorAnalyzerWrapper(Ca caInstance)
        {
            _caInstance = caInstance ?? throw new ArgumentNullException(nameof(caInstance));
        }

        // Реализация свойств из IColorAnalyzer
        public IColorAnalyzerProbe SingleProbe
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot access SingleProbe on an uninitialized object.");
                }
                _singleProbeWrapper ??= new ColorAnalyzerProbeWrapper(_caInstance.SingleProbe);
                return _singleProbeWrapper;
            }
        }

        public IColorAnalyzerMemory Memory
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot access Memory on an uninitialized object.");
                }
                _memoryWrapper ??= new ColorAnalyzerMemoryWrapper(_caInstance.Memory);
                return _memoryWrapper;
            }
        }

        public void CalZero()
        {
            if (_caInstance == null)
            {
                throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot call CalZero on an uninitialized object.");
            }
            _caInstance.CalZero();
        }

        public int SyncMode
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot access SyncMode on an uninitialized object.");
                }
                return (int)_caInstance.SyncMode;
            }
            set
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot set SyncMode on an uninitialized object.");
                }
                _caInstance.SyncMode = value;
            }
        }

        public int AveragingMode
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot access AveragingMode on an uninitialized object.");
                }
                return _caInstance.AveragingMode;
            }
            set
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot set AveragingMode on an uninitialized object.");
                }
                _caInstance.AveragingMode = value;
            }
        }

        public void SetAnalogRange(float Range1, float Range2)
        {
            if (_caInstance == null)
            {
                throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot call SetAnalogRange on an uninitialized object.");
            }
            _caInstance.SetAnalogRange(Range1, Range2);
        }

        public int DisplayMode
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot access DisplayMode on an uninitialized object.");
                }
                return _caInstance.DisplayMode;
            }
            set
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot set DisplayMode on an uninitialized object.");
                }
                _caInstance.DisplayMode = value;
            }
        }

        public void Measure()
        {
            if (_caInstance == null)
            {
                throw new ObjectDisposedException(nameof(ColorAnalyzerWrapper), "Cannot call Measure on an uninitialized object.");
            }
            _caInstance.Measure();
        }

        // Вложенные обертки (Probe, Memory) не владеют своими COM-объектами напрямую,
        // они получают их от _caInstance. Поэтому здесь достаточно обнулить ссылки.
        public void Dispose()
        {
            _singleProbeWrapper = null;
            _memoryWrapper = null;
            _caInstance = null; // Обнуляем ссылку на внутренний COM-объект
        }
    }

    /// <summary>
    /// Обёртка для объекта CA200SRVRLib.Ca200, реализующая IColorAnalyzer200.
    /// Отвечает за создание и корректное освобождение корневого COM-объекта Ca200.
    /// </summary>
    public class ColorAnalyzer200Wrapper : IColorAnalyzer200
    {
        private Ca200? _ca200Instance; // Реальный COM-объект CA200SRVRLib.Ca200
        private ColorAnalyzerWrapper? _singleCaWrapper; // Обёртка для IColorAnalyzer
        private bool _disposed = false; // Флаг для реализации IDisposable

        public ColorAnalyzer200Wrapper()
        {
            try
            {
                _ca200Instance = new Ca200(); // Создаем реальный COM-объект
            }
            catch (COMException ex)
            {
                // Для production-кода здесь лучше использовать систему логирования,
                // а не Console.Error.WriteLine.
                Console.Error.WriteLine($"ColorAnalyzerWrappers: COM Error creating Ca200: {ex.Message}");
                _ca200Instance = null; // Убеждаемся, что объект null в случае ошибки
                throw; // Пробрасываем ошибку, т.к. без базового объекта работать не сможем
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ColorAnalyzerWrappers: Error creating Ca200: {ex.Message}");
                _ca200Instance = null;
                throw;
            }
        }

        // Реализация свойства SingleCa из IColorAnalyzer200
        public IColorAnalyzer SingleCa
        {
            get
            {
                if (_disposed || _ca200Instance == null)
                {
                    throw new ObjectDisposedException(nameof(ColorAnalyzer200Wrapper), "Cannot access SingleCa on a disposed or uninitialized object.");
                }
                // Создаем обертку для SingleCa только при первом запросе
                _singleCaWrapper ??= new ColorAnalyzerWrapper(_ca200Instance.SingleCa);
                return _singleCaWrapper;
            }
        }

        // Реализация метода AutoConnect из IColorAnalyzer200
        public void AutoConnect()
        {
            if (_disposed || _ca200Instance == null)
            {
                throw new ObjectDisposedException(nameof(ColorAnalyzer200Wrapper), "Cannot call AutoConnect on a disposed or uninitialized object.");
            }
            _ca200Instance.AutoConnect();
        }

        // Реализация IDisposable для корректной очистки COM-объекта
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Говорим сборщику мусора не вызывать финализатор
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return; // Защита от повторных вызовов

            if (disposing)
            {
                // Если SingleCaWrapper был создан, вызываем его Dispose, чтобы он обнулил свои ссылки
                if (_singleCaWrapper != null)
                {
                    _singleCaWrapper.Dispose(); // Вызываем Dispose обертки Ca
                    _singleCaWrapper = null;
                }
            }

            // Освобождаем неуправляемые ресурсы (COM-объект Ca200)
            if (_ca200Instance != null)
            {
                try
                {
                    if (Marshal.IsComObject(_ca200Instance))
                    {
                        // КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: Используем FinalReleaseComObject
                        Marshal.FinalReleaseComObject(_ca200Instance);
                    }
                }
                catch (Exception ex)
                {
                    // Для production-кода здесь лучше использовать систему логирования,
                    // а не Console.Error.WriteLine.
                    Console.Error.WriteLine($"ColorAnalyzerWrappers: Error during Marshal.FinalReleaseComObject for _ca200Instance: {ex.Message}");
                }
                _ca200Instance = null; // Обнуляем ссылку после попытки освобождения
            }

            // Дополнительная агрессивная очистка, если необходима
            GC.Collect();
            GC.WaitForPendingFinalizers();
            System.Threading.Thread.Sleep(100); // Небольшая задержка

            _disposed = true; // Устанавливаем флаг, что Dispose выполнен
        }

        // Финализатор (деструктор) для случая, если Dispose не был вызван явно
        ~ColorAnalyzer200Wrapper()
        {
            Dispose(false);
        }
    }
}