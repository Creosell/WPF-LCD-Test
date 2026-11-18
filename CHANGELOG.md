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
- **Attempts of a point**: every accepted measurement of a point is kept as an attempt (no limit, time included) and the point holds the best one; a measurement rejected by the analyzer validation is not an attempt and leaves the earlier attempts untouched (only the button turns red, its specification badge is hidden). The best attempt is chosen by the specification check (`ReportGenerator.exe` 1.5.0): the best status first, then the higher luminance (the red/green/blue points: closer to the typical coordinates), without a specification the brightest. The Results table, Device info, the badges and the saved file use the best attempt; the saved JSON carries all attempts of a point measured several times in an optional `Attempts` list (`Time`, `x`, `y`, `Lv`, `T`, `Best`). The new **Tries** column of the Results table shows a dash until an attempt of the point had a remark against the specification, then `N/3` (amber below three attempts, green from three). The log line of a remark names the point and the attempt (`Center: Brightness 251 < min 290 (attempt 1)`), outside min/max it adds that repeating the measurement is recommended
- **Specification check while measuring**: after every accepted measurement (and on a configuration or TV change) the application runs `ReportGenerator.exe --evaluate -` (about 0.35 s, in the background, 5 s timeout) with the device JSON on stdin, so the rules of the live check and of the final report are the same. The **Device info** panel next to the log lists the metrics of the selected configuration (brightness, uniformity, contrast, temperature, gamut, ΔE) with value, spec and a status color, a verdict chip (`Spec: FAIL / WARN / PASS / no issues yet`) and the number of measured points; the rows of metrics whose points are not measured yet stay gray. A measurement button gets a badge (amber exclamation mark - below typ, red cross - outside min/max) with a tooltip about its single-point checks (coordinates, brightness, temperature, the darkest uniformity point); the badge is hidden while the point holds no accepted measurement. New deviations are also written to the log. Saving asks for confirmation when any value is outside the specification or below the typical one: the remarks are listed (the points with their number of attempts, "1 of 3"), with the advice to measure again when a point got fewer than three attempts. The check is skipped with a log line when the generator is missing, the specification is not found or the call fails
- Optional `Colorimeter` object (`Model`, `ProbeSN`, `Channel`) in the device JSON header, filled once after calibration; files without it keep the previous format

### Changed
- The block left of Device info has two tabs, with white headers, a purple line under the selected one and a thin light line on hover. **Results** is a table of all points (x, y, Lv, T of the accepted measurement, dashes otherwise) whose dot has the color of the point's button; a point that does not exist for the device (white for a non-TV) is hidden, a rejected measurement is tinted red (the reason is in the log). **Log** holds only events: calibration, serial number, rejected measurements with the reason, specification deviations, saving, reports and errors, left-aligned and colored by severity (amber - warning, red - error); the tab shows a red counter of warnings and errors added while it was not open. The "Measuring..." and per-point "Result for ..." lines and the welcome line are no longer written (the button state, the Results tab and the analyzer card show that); the log context menu got Copy log
- The application is built as x64; the CA-SDK2 libraries are not shipped: they are located in the installed CA-S40 package (registry install location, then the default folder) and the CA-410 driver is skipped when the package is absent
- CA-310 wrapper classes renamed to `Ca310*` (`ColorAnalyzerWrappers.cs` -> `Ca310Wrappers.cs`)

### Removed
- The Cancel button of the settings view (it only reloaded unsaved edits), replaced by Reset to default (measurement speed and sync mode back to Slow / UNIV; saved with Save)
- Hardcoded QA probe handling (serial number `08954195`, forced channel 1)
- The unused `autoConnectEnabled` setting (nothing read it); old settings files that still contain it load fine

### Fixed
- The dotted keyboard focus rectangle drawn around views and controls is removed in the whole application
- A picked value in the settings view is no longer left highlighted: the focus returns to the view when a list closes or when the click lands outside the inputs
- Feedback of the control buttons now that the log is on a hidden tab: Save results, Clear results, Create report and Upload reports breathe once, smoothly, green when their action succeeded and red when it failed or was canceled (a short inhale, the color is held for a moment with the outcome on the label: Saved, Cleared, Done, Uploaded, Failed, Canceled, and a twice as slow exhale); declining a confirmation dialog shows nothing. While a report is created or uploaded the button shows "Processing..." / "Uploading..." and stays clickable: a second click cancels the action (the generator or the upload CLI processes are killed), logs it and breathes red
- Message and confirmation dialogs are WPF windows instead of the native message box, so the feedback animation of a button no longer freezes while a dialog (for example the clear-results question after saving) is open
- The Save results button no longer looks disabled (greyed) while the clear-results question is open after saving: the command stays enabled and repeated clicks are ignored while it runs
- Saving asks one question: the "not all measurements have been taken" notice and the specification remarks are shown together in a single dialog instead of two consecutive ones
- The outcome label on a control button (Saved, Done, ...) stays through the whole exhale and then fades into the button's own text
- Saving results is disabled (the Save results button is inactive, like the measurement buttons without a serial number) until a device configuration is selected
- The underline of the serial number field and of the device configuration selector breathes quickly (about 1.6 s) green (the serial number field uses a separate line laid over its underline, so the hover and focus highlight of the field cannot hide it) when a serial number is applied or a configuration is chosen (red for a rejected serial number)
- Hotkeys: Ctrl+S saves the results (like the Save results button), Ctrl+R creates the report
- Closing the application with unsaved measurements asks for confirmation
- The Upload reports button shows how many reports wait for upload, for example "Upload reports (3)"
- The Log tab lists only warnings and errors with a compact time (HH:mm); the copied log text still contains everything with seconds
- In the confirmation dialogs the last paragraph (the question) is shown apart at the bottom
- The underline of the device configuration selector uses the same border and hover colors as the serial number field (it was black)
- The Language label in Settings reads "Language / 语言" in both languages, so it can be found which language is active
- Switching between the Measurement and Settings pages took 100-350 ms because the page was rebuilt each time (the DataTemplate of a ContentControl); both pages are now created once and switched by visibility (2-5 ms)
- Changing the language reset the other settings (channel, auto connect) to their defaults; only the language is updated now
- The CA-410 ran in LTD.AUTO instead of AUTO because the CA-310 speed constant was passed to it
- An infinite color temperature (CA-410 reports it for saturated colors) is shown as `T=n/a` in the log and in the button tooltip
- Saving the device JSON failed when a measurement contained an infinite value (CA-410 reports an infinite color temperature for saturated colors); non-finite values are now written as `null`
- A repeated zero calibration kept the device reported as calibrated for the whole new calibration; it is now reported as uncalibrated until the calibration finishes
- The measurement buttons and the Device info rows showed an empty tooltip box when there was nothing to tell (before the first measurement, for rows without remarks); a tooltip now appears only when it has text
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
