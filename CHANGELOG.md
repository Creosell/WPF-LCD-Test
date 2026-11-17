# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.4.0] - 2026-11-17

### Added
- **Konica Minolta CA-410 support** through CA-SDK2 (`CASDK2Net.dll`, x64): connection, zero calibration, channel selection and measurements. The CA-410 is connected directly to the measuring probe, so only the probe serial number is available. The legacy CA-310 (COM) driver is kept as a fallback; the device family is detected automatically during zero calibration (CA-410 first, then CA-310)
- Zero calibration reads the active channel from the probe instead of overwriting it with the Settings value; the confirmed channel is saved to the settings
- `analyzers.json` next to the executable remembers the last confirmed channel per probe serial number. When the probe channel differs from the remembered one, a dialog lets the inspector pick either of the two; closing it cancels the calibration
- Device card in the measurement view: model, active channel and probe serial number (shown only after the channel is confirmed) with a single state dot (gray - not connected, amber - connected, green - connected and calibrated) and a full state tooltip, replacing the separate Connected/Calibrated texts. Before the channel is confirmed the card shows a dimmed "No analyzer / Press Zero Calibration" hint or "Calibrating…" instead of empty placeholders; switching between the states is smooth (old content fades out, the card eases to the new width, new content fades in) and the amber dot gently breathes while the analyzer is calibrating
- Measurement speed (Slow / Fast / Auto) and synchronization mode settings, applied during zero calibration. Defaults: Slow and Universal. Sync modes: NTSC, PAL, EXT, UNIV, INT (frequency field shown only for INT) and, for the CA-410 only, MANUAL (integration time field shown only for MANUAL). The list and the value ranges follow the connected analyzer (INT 40-200 Hz on the CA-310, 0.5-240 Hz on the CA-410; MANUAL 4-4000 ms); a stored setting the connected analyzer does not support is replaced by UNIV with a log message. The CA-410 driver translates Auto to its native value (3), which differs from the CA-310 (2)
- Optional `Colorimeter` object (`Model`, `ProbeSN`, `Channel`) in the device JSON header, filled once after calibration; files without it keep the previous format

### Changed
- The application is built as x64; the CA-SDK2 libraries are not shipped: they are located in the installed CA-S40 package (registry install location, then the default folder) and the CA-410 driver is skipped when the package is absent
- CA-310 wrapper classes renamed to `Ca310*` (`ColorAnalyzerWrappers.cs` -> `Ca310Wrappers.cs`)

### Removed
- The Cancel button of the settings view (it only reloaded unsaved edits), replaced by Reset to default (measurement speed and sync mode back to Slow / UNIV; saved with Save)
- Hardcoded QA probe handling (serial number `08954195`, forced channel 1)
- The unused `autoConnectEnabled` setting (nothing read it); old settings files that still contain it load fine

### Fixed
- The dotted keyboard focus rectangle drawn around views and controls is removed in the whole application
- A picked value in the settings view is no longer left highlighted: the focus returns to the view when a list closes or when the click lands outside the inputs
- Changing the language reset the other settings (channel, auto connect) to their defaults; only the language is updated now
- The CA-410 ran in LTD.AUTO instead of AUTO because the CA-310 speed constant was passed to it
- Saving the device JSON failed when a measurement contained an infinite value (CA-410 reports an infinite color temperature for saturated colors); non-finite values are now written as `null`
- A repeated zero calibration kept the device reported as calibrated for the whole new calibration; it is now reported as uncalibrated until the calibration finishes
- The interface shifted down by a few pixels after a device configuration was selected (the placeholder used a smaller font than the selected name)

---

## [1.3.0] - 2026-08-11

### Added
- Hierarchical device configuration menu: config files nested in subfolders now render as a cascading flyout menu instead of a flat dropdown, keeping configs for different orders visually separated
- Checkmark indicator for the currently selected configuration, shown at any nesting depth

---

## [1.2.4] - 2026-05-07

### Changed
- Application renamed from **WPF LCD Test** to **Screen Checker**
- Window title now displays version number dynamically (e.g. `Screen Checker 1.2.4`)
- Added release tooling: `tools/release/release_manager.py` for packaging and uploading builds to Nextcloud
- Updated README: project renamed, corrected build/test commands, added Releasing section

---

## [1.2.3] - 2026-02-03

### Changed
- Color validation tolerance increased from **0.1 to 0.15** (`COLOR_COORDINATES_TOLERANCE`)
- Refactored calibration flow to use unified `ExecuteConnectAsync` for consistent error handling across connect and calibrate operations

---

## [1.2.2] - 2026-01-28

### Added
- COM object lifecycle management tests in `ColorMeasurementServiceTests`
- Tests for `FileService` and `UploadService` covering error scenarios

### Fixed
- Critical memory leaks: `Process` disposal in file operations, event subscription cleanup, log output capped at 500 lines
- Race condition in calibration causing the calibration step to be skipped after initial device connection
- Forced device disconnect on calibration/connection errors for proper COM object cleanup
- XAML data binding regression introduced during ViewModel refactoring

---

## [1.2.1] - 2026-01-26

### Added
- XML documentation for all public methods and service interfaces

### Changed
- Fully migrated from Lazy singleton pattern to **Microsoft.Extensions.DependencyInjection** container with Singleton/Transient lifetimes
- Replaced custom file system wrappers with **System.IO.Abstractions** (`IFileSystem` / `MockFileSystem`) for consistent testability
- Centralized application-wide constants into `AppConstants` class (`ColorAnalyzer.MinChannel`, `ColorAnalyzer.MaxChannel`)
- Centralized path management via `IPathProvider` / `PathProvider` — eliminates hardcoded `AppDomain.CurrentDomain.BaseDirectory`
- Refactored `MeasurementViewModel` for clarity and modern C# idioms

---

## [1.2.0] - 2025-11-19

### Added
- Parallel report upload to Nextcloud server (`UploadService`) with `Task.Run` and folder scanning
- Upload result archiving: processed reports moved to archive folder after successful upload

### Changed
- Localization refactored: Chinese Simplified resources fully separated into `StringResources.zh-Hans.xaml`
- Removed remaining hardcoded UI strings; all user-visible text routed through `ILocalizationService`
- Runtime language switching stabilized with proper resource dictionary reload

---

## [1.1.0] - 2025-10-15

### Added
- Multi-channel measurement switch support (channels 0–99 via `AppConstants.ColorAnalyzer`)
- QA probe serial number detection (`QA_PROBE_SN` constant in `MeasurementViewModel`)
- Color coordinate validation for RGB primaries and 9 white point positions
- Measurement retry logic with `ConcurrentDictionary` for thread-safe per-location attempt tracking

### Changed
- Improved CA-310 connection reliability
- Reworked measurement status tracking moved to dedicated `MeasurementStatusService`

---

## [1.0.0] - 2025-04-28

### Added
- Initial release: WPF desktop application for LCD color testing
- Konica Minolta CA-200/CA-310 COM interop via `CA200SRVRLib`
- MVVM architecture with Material Design UI (English / Chinese Simplified)
- Measurement of brightness, contrast, color gamut, RGB primaries, white point, color temperature
- Device configuration via YAML files (`config/device_configs/`)
- Measurement results export to JSON and CSV
- Interface-based services with Moq-compatible wrappers for testability
- NUnit 3 test suite covering ViewModels, Services, and Models
