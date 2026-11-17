using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WPF_LCD_Test.Wrappers
    {
    /// <summary>
    /// Locates the CA-SDK2 libraries installed by the official Konica Minolta setup (CA-S40) and makes them loadable,
    /// so they do not have to be shipped with the application.
    /// </summary>
    internal static class Ca410SdkLoader
        {
        private const string ManagedLibraryName = "CASDK2Net";
        private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{9307D78D-74A8-4520-8C1C-EF82D8E21EC6}";
        private const string DefaultInstallDirectory = @"C:\Program Files (x86)\KONICA MINOLTA\CA-S40\";
        private const string LibrarySubdirectory = @"CA-SDK2\x64\lib";

        private static readonly object Sync = new();
        private static bool _isInitialized;
        private static string? _libraryDirectory;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string path);

        /// <summary>
        /// Registers the SDK folder for managed and native library resolution (once per process).
        /// </summary>
        /// <returns>True when the CA-SDK2 libraries were found and registered; false when the SDK is not installed.</returns>
        public static bool TryInitialize()
            {
            lock (Sync)
                {
                if (_isInitialized) return _libraryDirectory != null;
                _isInitialized = true;

                _libraryDirectory = FindLibraryDirectory();
                if (_libraryDirectory == null) return false;

                SetDllDirectory(_libraryDirectory);
                AppDomain.CurrentDomain.AssemblyResolve += ResolveManagedLibrary;
                return true;
                }
            }

        /// <summary>
        /// Looks for the SDK folder next to the application, then in the registered install location, then in the default one.
        /// </summary>
        private static string? FindLibraryDirectory()
            {
            var candidates = new[]
                {
                AppContext.BaseDirectory,
                Path.Combine(GetInstallLocation() ?? DefaultInstallDirectory, LibrarySubdirectory),
                Path.Combine(DefaultInstallDirectory, LibrarySubdirectory)
                };

            return candidates.FirstOrDefault(directory => File.Exists(Path.Combine(directory, ManagedLibraryName + ".dll")));
            }

        /// <summary>
        /// Reads the CA-S40 install location from the uninstall registry entry of either registry view.
        /// </summary>
        private static string? GetInstallLocation()
            {
            foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
                {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var key = baseKey.OpenSubKey(UninstallKeyPath);
                if (key?.GetValue("InstallLocation") is string location && !string.IsNullOrWhiteSpace(location))
                    {
                    return location;
                    }
                }
            return null;
            }

        private static Assembly? ResolveManagedLibrary(object? sender, ResolveEventArgs args)
            {
            if (_libraryDirectory == null || new AssemblyName(args.Name).Name != ManagedLibraryName) return null;

            return Assembly.LoadFrom(Path.Combine(_libraryDirectory, ManagedLibraryName + ".dll"));
            }
        }
    }
