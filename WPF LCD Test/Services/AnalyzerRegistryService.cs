using System.Diagnostics;
using System.IO.Abstractions;
using System.Text.Json;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Services
    {
    /// <summary>
    /// Stores the probe serial number to last used channel registry in a JSON file next to the application settings.
    /// </summary>
    public class AnalyzerRegistryService : IAnalyzerRegistryService
        {
        private const string RegistryFileName = "analyzers.json";

        private static readonly JsonSerializerOptions _serializerOptions = new()
            {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            PropertyNameCaseInsensitive = true
            };

        private readonly IFileSystem _fileSystem;
        private readonly IPathProvider _pathProvider;
        private readonly object _sync = new();

        /// <summary>
        /// Initializes a new instance of AnalyzerRegistryService.
        /// </summary>
        /// <param name="fileSystem">File system abstraction.</param>
        /// <param name="pathProvider">Path provider used to resolve the base directory.</param>
        public AnalyzerRegistryService(IFileSystem fileSystem, IPathProvider pathProvider)
            {
            _fileSystem = fileSystem;
            _pathProvider = pathProvider;
            }

        private string RegistryFilePath => _fileSystem.Path.Combine(_pathProvider.BaseDirectory, RegistryFileName);

        /// <inheritdoc/>
        public int? GetLastChannel(string probeSN)
            {
            lock (_sync)
                {
                return Find(Load(), probeSN)?.LastChannel;
                }
            }

        /// <inheritdoc/>
        public void Update(string probeSN, ColorAnalyzerModel model, int channel)
            {
            lock (_sync)
                {
                var entries = Load();
                var entry = Find(entries, probeSN);
                if (entry == null)
                    {
                    entry = new AnalyzerRegistryEntry { ProbeSN = probeSN };
                    entries.Add(entry);
                    }

                entry.Model = model;
                entry.LastChannel = channel;
                entry.LastSeenUtc = DateTime.UtcNow;
                Save(entries);
                }
            }

        /// <summary>
        /// Finds the entry of the given probe, ignoring serial number case.
        /// </summary>
        /// <param name="entries">Registry entries.</param>
        /// <param name="probeSN">Probe serial number.</param>
        /// <returns>The matching entry, or null.</returns>
        private static AnalyzerRegistryEntry? Find(List<AnalyzerRegistryEntry> entries, string probeSN) =>
            entries.FirstOrDefault(e => string.Equals(e.ProbeSN, probeSN, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Reads all registry entries; a missing or unreadable file yields an empty registry.
        /// </summary>
        /// <returns>Mutable list of registry entries.</returns>
        private List<AnalyzerRegistryEntry> Load()
            {
            try
                {
                if (!_fileSystem.File.Exists(RegistryFilePath)) return [];
                var json = _fileSystem.File.ReadAllText(RegistryFilePath);
                return JsonSerializer.Deserialize<List<AnalyzerRegistryEntry>>(json, _serializerOptions) ?? [];
                }
            catch (Exception ex)
                {
                Debug.WriteLine($"Failed to read analyzer registry: {ex.Message}");
                return [];
                }
            }

        /// <summary>
        /// Writes all registry entries to disk; failures are logged and do not interrupt the measurement flow.
        /// </summary>
        /// <param name="entries">Entries to persist.</param>
        private void Save(List<AnalyzerRegistryEntry> entries)
            {
            try
                {
                                _fileSystem.File.WriteAllText(RegistryFilePath, JsonSerializer.Serialize(entries, _serializerOptions));
                }
            catch (Exception ex)
                {
                Debug.WriteLine($"Failed to save analyzer registry: {ex.Message}");
                }
            }
        }
    }
