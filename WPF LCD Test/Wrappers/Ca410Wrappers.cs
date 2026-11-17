using CASDK2;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Wrappers
    {
    /// <summary>
    /// Translates CA-SDK2 status codes into exceptions.
    /// </summary>
    internal static class Ca410Sdk
        {
        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> carrying the localized SDK message when the call failed.
        /// </summary>
        /// <param name="code">Status code returned by an SDK call (0 means success).</param>
        /// <param name="operation">Name of the SDK operation, used in the exception message.</param>
        public static void Check(int code, string operation)
            {
            if (code == 0) return;

            string message = string.Empty;
            GlobalFunctions.CASDK2_GetLocalizedErrorMsgFromErrorCode(0, code, ref message);
            throw new InvalidOperationException($"CA-SDK2 {operation} failed ({code}): {message}");
            }
        }

    /// <summary>
    /// <see cref="IColorAnalyzerMemory"/> implementation over a CA-SDK2 memory object.
    /// </summary>
    public class Ca410MemoryWrapper : IColorAnalyzerMemory, IDisposable
        {
        private CASDK2Memory? _memory;

        /// <summary>
        /// Initializes the wrapper.
        /// </summary>
        /// <param name="memory">SDK memory object.</param>
        public Ca410MemoryWrapper(CASDK2Memory memory) => _memory = memory ?? throw new ArgumentNullException(nameof(memory));

        /// <inheritdoc/>
        public int ChannelNO
            {
            get
                {
                int channel = 0;
                Ca410Sdk.Check(Memory.get_ChannelNO(ref channel), nameof(ChannelNO));
                return channel;
                }
            set => Ca410Sdk.Check(Memory.put_ChannelNO(value), nameof(ChannelNO));
            }

        private CASDK2Memory Memory => _memory ?? throw new ObjectDisposedException(nameof(Ca410MemoryWrapper));

        /// <inheritdoc/>
        public void Dispose()
            {
            _memory?.Dispose();
            _memory = null;
            }
        }

    /// <summary>
    /// <see cref="IColorAnalyzerProbe"/> implementation over a CA-SDK2 probe object.
    /// </summary>
    public class Ca410ProbeWrapper : IColorAnalyzerProbe, IDisposable
        {
        private CASDK2Probe? _probe;

        /// <summary>
        /// Initializes the wrapper.
        /// </summary>
        /// <param name="probe">SDK probe object.</param>
        public Ca410ProbeWrapper(CASDK2Probe probe) => _probe = probe ?? throw new ArgumentNullException(nameof(probe));

        /// <inheritdoc/>
        public double sx => ReadDouble(nameof(sx), (CASDK2Probe p, ref double v) => p.get_sx(ref v));

        /// <inheritdoc/>
        public double sy => ReadDouble(nameof(sy), (CASDK2Probe p, ref double v) => p.get_sy(ref v));

        /// <inheritdoc/>
        public double Lv => ReadDouble(nameof(Lv), (CASDK2Probe p, ref double v) => p.get_Lv(ref v));

        /// <inheritdoc/>
        public double T => ReadDouble(nameof(T), (CASDK2Probe p, ref double v) => p.get_T(ref v));

        /// <inheritdoc/>
        public string SerialNO => ReadString(nameof(SerialNO), (CASDK2Probe p, ref string v) => p.get_SerialNO(ref v));

        /// <inheritdoc/>
        public string TypeName => ReadString(nameof(TypeName), (CASDK2Probe p, ref string v) => p.get_TypeName(ref v));

        private delegate int DoubleGetter(CASDK2Probe probe, ref double value);
        private delegate int StringGetter(CASDK2Probe probe, ref string value);

        private CASDK2Probe Probe => _probe ?? throw new ObjectDisposedException(nameof(Ca410ProbeWrapper));

        private double ReadDouble(string name, DoubleGetter getter)
            {
            double value = 0;
            Ca410Sdk.Check(getter(Probe, ref value), name);
            return value;
            }

        private string ReadString(string name, StringGetter getter)
            {
            string value = string.Empty;
            Ca410Sdk.Check(getter(Probe, ref value), name);
            return value;
            }

        /// <inheritdoc/>
        public void Dispose()
            {
            _probe?.Dispose();
            _probe = null;
            }
        }

    /// <summary>
    /// <see cref="IColorAnalyzer"/> implementation over a CA-SDK2 analyzer object.
    /// </summary>
    public class Ca410Wrapper : IColorAnalyzer, IDisposable
        {
        private const int AutoSpeed = (int)MeasurementSpeed.Auto;
        private const int NativeAutoSpeed = 3;

        private CASDK2Ca? _ca;
        private Ca410ProbeWrapper? _probe;
        private Ca410MemoryWrapper? _memory;

        /// <summary>
        /// Initializes the wrapper.
        /// </summary>
        /// <param name="ca">SDK analyzer object.</param>
        public Ca410Wrapper(CASDK2Ca ca) => _ca = ca ?? throw new ArgumentNullException(nameof(ca));

        private CASDK2Ca Ca => _ca ?? throw new ObjectDisposedException(nameof(Ca410Wrapper));

        /// <inheritdoc/>
        public IColorAnalyzerProbe SingleProbe => _probe ??= CreateProbe();

        /// <inheritdoc/>
        public IColorAnalyzerMemory Memory => _memory ??= CreateMemory();

        /// <inheritdoc/>
        public int Number
            {
            get
                {
                int value = 0;
                Ca410Sdk.Check(Ca.get_Number(ref value), nameof(Number));
                return value;
                }
            }

        /// <inheritdoc/>
        public string ID
            {
            get
                {
                string value = string.Empty;
                Ca410Sdk.Check(Ca.get_ID(ref value), nameof(ID));
                return value;
                }
            }

        /// <inheritdoc/>
        public string PortID
            {
            get
                {
                string value = string.Empty;
                Ca410Sdk.Check(Ca.get_PortID(ref value), nameof(PortID));
                return value;
                }
            }

        /// <inheritdoc/>
        public SyncSetting Sync
            {
            get
                {
                int mode = 0;
                double value = 0;
                Ca410Sdk.Check(Ca.get_SyncMode(ref mode, ref value), nameof(Sync));
                return new SyncSetting((SyncMode)mode, value);
                }
            set
                {
                if (!AnalyzerCapabilities.IsSupported(ColorAnalyzerModel.CA410, value))
                    {
                    throw new NotSupportedException($"CA-410 does not support the sync setting {value.Mode} ({value.Value}).");
                    }
                Ca410Sdk.Check(Ca.put_SyncMode((int)value.Mode, value.Value), nameof(Sync));
                }
            }

        /// <inheritdoc/>
        public int AveragingMode
            {
            get
                {
                int mode = 0;
                Ca410Sdk.Check(Ca.get_AveragingMode(ref mode), nameof(AveragingMode));
                return mode == NativeAutoSpeed ? AutoSpeed : mode;
                }
            set => Ca410Sdk.Check(Ca.put_AveragingMode(value == AutoSpeed ? NativeAutoSpeed : value), nameof(AveragingMode));
            }

        /// <inheritdoc/>
        public int DisplayMode
            {
            get
                {
                int mode = 0;
                Ca410Sdk.Check(Ca.get_DisplayMode(ref mode), nameof(DisplayMode));
                return mode;
                }
            set => Ca410Sdk.Check(Ca.put_DisplayMode(value), nameof(DisplayMode));
            }

        /// <inheritdoc/>
        public void CalZero() => Ca410Sdk.Check(Ca.CalZero(), nameof(CalZero));

        /// <inheritdoc/>
        public void Measure() => Ca410Sdk.Check(Ca.Measure(), nameof(Measure));

        /// <inheritdoc/>
        public void SetAnalogRange(float Range1, float Range2) => Ca410Sdk.Check(Ca.SetAnalogRange(Range1, Range2), nameof(SetAnalogRange));

        private Ca410ProbeWrapper CreateProbe()
            {
            CASDK2Probe? probe = null;
            Ca410Sdk.Check(Ca.get_SingleProbe(ref probe), nameof(SingleProbe));
            return new Ca410ProbeWrapper(probe!);
            }

        private Ca410MemoryWrapper CreateMemory()
            {
            CASDK2Memory? memory = null;
            Ca410Sdk.Check(Ca.get_Memory(ref memory), nameof(Memory));
            return new Ca410MemoryWrapper(memory!);
            }

        /// <inheritdoc/>
        public void Dispose()
            {
            _probe?.Dispose();
            _probe = null;
            _memory?.Dispose();
            _memory = null;
            _ca?.Dispose();
            _ca = null;
            }
        }

    /// <summary>
    /// <see cref="IColorAnalyzer200"/> implementation over the CA-SDK2 root application object (CA-410).
    /// </summary>
    public class Ca410AnalyzerWrapper : IColorAnalyzer200
        {
        private CASDK2Ca200? _ca200 = new();
        private Ca410Wrapper? _singleCa;
        private bool _connected;

        /// <inheritdoc/>
        public ColorAnalyzerModel Model => ColorAnalyzerModel.CA410;

        /// <inheritdoc/>
        public IColorAnalyzer SingleCa => _singleCa ??= CreateSingleCa();

        /// <inheritdoc/>
        public void AutoConnect()
            {
            Ca410Sdk.Check(Root.AutoConnect(), nameof(AutoConnect));
            _connected = true;
            }

        private CASDK2Ca200 Root => _ca200 ?? throw new ObjectDisposedException(nameof(Ca410AnalyzerWrapper));

        private Ca410Wrapper CreateSingleCa()
            {
            CASDK2Ca? ca = null;
            Ca410Sdk.Check(Root.get_SingleCa(ref ca), nameof(SingleCa));
            return new Ca410Wrapper(ca!);
            }

        /// <inheritdoc/>
        public void Dispose()
            {
            _singleCa?.Dispose();
            _singleCa = null;

            if (_ca200 != null)
                {
                if (_connected) _ca200.AutoDisconnect();
                _ca200.Dispose();
                _ca200 = null;
                }
            _connected = false;
            }
        }
    }
