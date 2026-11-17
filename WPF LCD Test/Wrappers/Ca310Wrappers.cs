// В папке Wrappers
// Файл Ca310Wrapper.cs
using CA200SRVRLib; // Прямая ссылка на COM-библиотеку
using System.Diagnostics;
using System.Runtime.InteropServices;
using WPF_LCD_Test.Interfaces; // Наши объединенные интерфейсы
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Wrappers
{
    /// <summary>
    /// Обёртка для объекта CA200SRVRLib.Memory, реализующая IColorAnalyzerMemory.
    /// </summary>
    public class Ca310MemoryWrapper : IColorAnalyzerMemory, IDisposable
    {
        private Memory? _memoryInstance; // Реальный COM-объект CA200SRVRLib.Memory

        public Ca310MemoryWrapper(Memory memoryInstance)
        {
            _memoryInstance = memoryInstance ?? throw new ArgumentNullException(nameof(memoryInstance));
        }

        // Реализация свойств из IColorAnalyzerMemory
        public int ChannelNO
        {
            get
            {
                if (_memoryInstance == null) throw new ObjectDisposedException(nameof(Ca310MemoryWrapper));
                return _memoryInstance.ChannelNO;
            }
            set
            {
                if (_memoryInstance == null) throw new ObjectDisposedException(nameof(Ca310MemoryWrapper));
                _memoryInstance.ChannelNO = value;
            }
        }

        /// <summary>
        /// Releases COM reference.
        /// </summary>
        public void Dispose()
        {
            if (_memoryInstance != null)
            {
                try
                {
                    if (Marshal.IsComObject(_memoryInstance))
                    {
                        Marshal.ReleaseComObject(_memoryInstance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to release ColorAnalyzerMemory COM object: {ex.Message}");
                }
                _memoryInstance = null;
            }
        }
    }

    /// <summary>
    /// Обёртка для объекта CA200SRVRLib.Probe, реализующая IColorAnalyzerProbe.
    /// </summary>
    public class Ca310ProbeWrapper : IColorAnalyzerProbe, IDisposable
    {
        private Probe? _probeInstance; // Реальный COM-объект CA200SRVRLib.Probe

        public Ca310ProbeWrapper(Probe probeInstance)
        {
            _probeInstance = probeInstance ?? throw new ArgumentNullException(nameof(probeInstance));
        }

        // Реализация свойств из IColorAnalyzerProbe
        public double sx
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(Ca310ProbeWrapper));
                return _probeInstance.sx;
            }
        }

        public double sy
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(Ca310ProbeWrapper));
                return _probeInstance.sy;
            }
        }

        public double Lv
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(Ca310ProbeWrapper));
                return _probeInstance.Lv;
            }
        }

        public double T
        {
            get
            {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(Ca310ProbeWrapper));
                return _probeInstance.T;
            }
        }

        public string SerialNO
            {
            get
                {
                if (_probeInstance == null) throw new ObjectDisposedException(nameof(Ca310ProbeWrapper));
                return _probeInstance.SerialNO;
                }
            }

        /// <inheritdoc/>
        public string TypeName => string.Empty;

        /// <summary>
        /// Releases COM reference.
        /// </summary>
        public void Dispose()
        {
            if (_probeInstance != null)
            {
                try
                {
                    if (Marshal.IsComObject(_probeInstance))
                    {
                        Marshal.ReleaseComObject(_probeInstance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to release ColorAnalyzerProbe COM object: {ex.Message}");
                }
                _probeInstance = null;
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
    public class Ca310Wrapper : IColorAnalyzer
    {
        private Ca? _caInstance; // Реальный COM-объект CA200SRVRLib.Ca
        private Ca310ProbeWrapper? _singleProbeWrapper; // Обёртка для IColorAnalyzerProbe
        private Ca310MemoryWrapper? _memoryWrapper; // Обёртка для IColorAnalyzerMemory 

        public Ca310Wrapper(Ca caInstance)
        {
            _caInstance = caInstance ?? throw new ArgumentNullException(nameof(caInstance));
            }


        public string ID
            {
            get
                {
                if (_caInstance == null)
                    {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access ID on an uninitialized object.");
                    }
                return _caInstance.ID;
                }
            }

        public string PortID
            {
            get
                {
                if (_caInstance == null)
                    {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access PortID on an uninitialized object.");
                    }
                return _caInstance.PortID;
                }
            }


        public int Number
            {
            get
                {
                if (_caInstance == null)
                    {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access Number on an uninitialized object.");
                    }
                return _caInstance.Number;
                }
            }

        // Реализация свойств из IColorAnalyzer
        public IColorAnalyzerProbe SingleProbe
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access SingleProbe on an uninitialized object.");
                }
                _singleProbeWrapper ??= new Ca310ProbeWrapper(_caInstance.SingleProbe);
                return _singleProbeWrapper;
            }
        }

        public IColorAnalyzerMemory Memory
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access Memory on an uninitialized object.");
                }
                _memoryWrapper ??= new Ca310MemoryWrapper(_caInstance.Memory);
                return _memoryWrapper;
            }
        }



        public void CalZero()
        {
            if (_caInstance == null)
            {
                throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot call CalZero on an uninitialized object.");
            }
            _caInstance.CalZero();
        }

        /// <summary>
        /// Gets or sets the sync mode. The COM property is a single value: 0-3 select NTSC, PAL, EXT and UNIV,
        /// 40-200 is the internal synchronization frequency in Hz.
        /// </summary>
        public SyncSetting Sync
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access SyncMode on an uninitialized object.");
                }
                float value = _caInstance.SyncMode;
                return value >= AnalyzerCapabilities.GetFrequencyRange(ColorAnalyzerModel.CA310).Min
                    ? new SyncSetting(SyncMode.Internal, value)
                    : new SyncSetting((SyncMode)(int)value);
            }
            set
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot set SyncMode on an uninitialized object.");
                }
                if (!AnalyzerCapabilities.IsSupported(ColorAnalyzerModel.CA310, value))
                {
                    throw new NotSupportedException($"CA-310 does not support the sync setting {value.Mode} ({value.Value}).");
                }
                _caInstance.SyncMode = value.Mode == SyncMode.Internal ? (float)value.Value : (float)(int)value.Mode;
            }
        }

        public int AveragingMode
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access AveragingMode on an uninitialized object.");
                }
                return _caInstance.AveragingMode;
            }
            set
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot set AveragingMode on an uninitialized object.");
                }
                _caInstance.AveragingMode = value;
            }
        }

        public void SetAnalogRange(float Range1, float Range2)
        {
            if (_caInstance == null)
            {
                throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot call SetAnalogRange on an uninitialized object.");
            }
            _caInstance.SetAnalogRange(Range1, Range2);
        }

        public int DisplayMode
        {
            get
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot access DisplayMode on an uninitialized object.");
                }
                return _caInstance.DisplayMode;
            }
            set
            {
                if (_caInstance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot set DisplayMode on an uninitialized object.");
                }
                _caInstance.DisplayMode = value;
            }
        }

        public void Measure()
        {
            if (_caInstance == null)
            {
                throw new ObjectDisposedException(nameof(Ca310Wrapper), "Cannot call Measure on an uninitialized object.");
            }
            _caInstance.Measure();
        }

        /// <summary>
        /// Releases all COM references held by this wrapper.
        /// </summary>
        public void Dispose()
        {
            // Dispose nested wrappers first
            if (_singleProbeWrapper != null)
            {
                try
                {
                    _singleProbeWrapper.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to dispose SingleProbe wrapper: {ex.Message}");
                }
                _singleProbeWrapper = null;
            }

            if (_memoryWrapper != null)
            {
                try
                {
                    _memoryWrapper.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to dispose Memory wrapper: {ex.Message}");
                }
                _memoryWrapper = null;
            }

            // Release COM object reference
            if (_caInstance != null)
            {
                try
                {
                    if (Marshal.IsComObject(_caInstance))
                    {
                        Marshal.ReleaseComObject(_caInstance);
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Ca310Wrapper: Error releasing COM object: {ex.Message}");
                }
                _caInstance = null;
            }
        }
    }

    /// <summary>
    /// Обёртка для объекта CA200SRVRLib.Ca200, реализующая IColorAnalyzer200.
    /// Отвечает за создание и корректное освобождение корневого COM-объекта Ca200.
    /// </summary>
    public class Ca310AnalyzerWrapper : IColorAnalyzer200
    {
        private Ca200? _ca200Instance; // Реальный COM-объект CA200SRVRLib.Ca200
        private Ca310Wrapper? _singleCaWrapper; // Обёртка для IColorAnalyzer
        private bool _disposed = false; // Флаг для реализации IDisposable

        public Ca310AnalyzerWrapper()
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

        /// <inheritdoc/>
        public ColorAnalyzerModel Model => ColorAnalyzerModel.CA310;

        // Реализация свойства SingleCa из IColorAnalyzer200
        public IColorAnalyzer SingleCa
        {
            get
            {
                if (_disposed || _ca200Instance == null)
                {
                    throw new ObjectDisposedException(nameof(Ca310AnalyzerWrapper), "Cannot access SingleCa on a disposed or uninitialized object.");
                }
                // Создаем обертку для SingleCa только при первом запросе
                _singleCaWrapper ??= new Ca310Wrapper(_ca200Instance.SingleCa);
                return _singleCaWrapper;
            }
        }

        // Реализация метода AutoConnect из IColorAnalyzer200
        public void AutoConnect()
        {
            if (_disposed || _ca200Instance == null)
            {
                throw new ObjectDisposedException(nameof(Ca310AnalyzerWrapper), "Cannot call AutoConnect on a disposed or uninitialized object.");
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
        ~Ca310AnalyzerWrapper()
        {
            Dispose(false);
        }
    }
}