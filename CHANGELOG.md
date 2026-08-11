# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
