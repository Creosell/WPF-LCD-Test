# Graph Report - WPF-LCD-Test  (2026-11-09)

## Corpus Check
- 78 files · ~40,237 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 2, .bmp 2, .resx 2)

## Summary
- 1324 nodes · 2843 edges · 82 communities (68 shown, 14 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 528 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c322e6f6`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Resources
- Test
- WPF_LCD_Test.Interfaces
- MeasurementStatus
- FileService
- UserControl
- MainWindowViewModelTests
- MeasurementViewModel
- DeviceUnderTest
- LogHandlerTests
- RelayCommand
- SettingsViewModel
- release_manager.py
- JsonSerializerOptions
- SettingsViewModelTests
- SettingsService
- MeasurementViewModel.Log
- ColorMeasurementServiceTests
- IColorAnalyzer
- App.ConfigureServices
- Screen Checker.UnitTests.csproj
- Window
- DialogService
- ColorAnalyzerWrapper
- MeasurementView
- AppSettings
- Changelog
- ColorMeasurementService
- UserControl
- DeviceConfigTreeNode
- CLAUDE.md
- IDispatcher
- ColorAnalyzer200Wrapper
- ISettingsService
- ColorAnalyzerMemoryWrapper
- ColorAnalyzerProbeWrapper
- MeasurementViewModel.UpdateUIState
- IColorMeasurementService
- Screen Checker
- main_tests
- SerialNumberTextBox
- Features
- Testing
- ReportExitCode
- [1.1.0] - 2025-10-15
- Releasing
- Development
- Localization
- Building the Project
- Usage
- Blue_x
- Blue_y
- Brightness
- Brightness_uniformity
- Cg_ntsc_area
- Cg_ntsc
- Cg_rgb_area
- Cg_rgb
- Contrast
- Delta_e
- Green_x
- Green_y
- Red_x
- Red_y
- Temperature
- White_y
- PART_Popup
- Resources.Designer.cs
- Configuration
- System Requirements
- Installation
- Running the Application
- IUploadService
- Bg
- StringResources.xaml
- StringResources.zh-Hans.xaml
- CheckGlyph
- SubmenuArrow

## God Nodes (most connected - your core abstractions)
1. `Resources` - 147 edges
2. `MeasurementViewModel` - 116 edges
3. `MeasurementViewModelTests` - 74 edges
4. `UserControl` - 65 edges
5. `Measurement` - 60 edges
6. `SettingsViewModel` - 45 edges
7. `WPF_LCD_Test.Interfaces` - 37 edges
8. `WPF_LCD_Test.Models` - 35 edges
9. `MeasurementStatus` - 35 edges
10. `SettingsViewModelTests` - 35 edges

## Surprising Connections (you probably didn't know these)
- `Changed` --references--> `ILocalizationService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Interfaces/ILocalizationService.cs
- `Changed` --references--> `MeasurementStatusService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Services/MeasurementStatusService.cs
- `Added` --references--> `UploadService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Services/UploadService.cs
- `Architecture` --references--> `RelayCommand`  [INFERRED]
  CLAUDE.md → WPF LCD Test/Commands/RelayCommand.cs
- `Wrappers` --references--> `IDispatcher`  [INFERRED]
  README.md → WPF LCD Test/Interfaces/IDispatcher.cs

## Import Cycles
- None detected.

## Communities (82 total, 14 thin omitted)

### Community 0 - "Resources"
Cohesion: 0.01
Nodes (144): Resources, Actual, AlreadyActivated, ArchiveFolderNotFound, Attempt, BadConnection, Batches, BlackPoint (+136 more)

### Community 1 - "Test"
Cohesion: 0.06
Nodes (14): Measurement, IsValid, Location, Lv, T, x, y, UploadReportItem (+6 more)

### Community 2 - "WPF_LCD_Test.Interfaces"
Cohesion: 0.06
Nodes (15): WPF_LCD_Test.Commands, WPF_LCD_Test.ViewModels, WPF_LCD_Test.Interfaces, WPF_LCD_Test.UnitTests.ViewModelsTests, WPF_LCD_Test.Services, WPF_LCD_Test.Converters, WPF_LCD_Test.UnitTests.ConvertersTests, WPF_LCD_Test.UnitTests.Mocks (+7 more)

### Community 3 - "MeasurementStatus"
Cohesion: 0.06
Nodes (29): IMeasurementStatusService, AllMeasurementButtonStatuses, Statuses, MeasurementLocation, BlackColor, BlueColor, BottomCenter, BottomLeft (+21 more)

### Community 4 - "FileService"
Cohesion: 0.06
Nodes (21): IFileService, BaseFolderPath, WorkFolderName, ILocalizationService, CurrentCulture, FileService, BaseFolderPath, WorkFolderName (+13 more)

### Community 5 - "UserControl"
Cohesion: 0.04
Nodes (52): Children, ClearLogCommand, BooleanToVisibilityConverter, DataContext.ApplySerialNumberCommand, DataContext.ClearFieldsCommand, DataContext.MeasureCommand, DataContext.MeasurementStatusService.Statuses[BlackColor].MeasuredValuesString, DataContext.MeasurementStatusService.Statuses[BlackColor].StatusColor (+44 more)

### Community 6 - "MainWindowViewModelTests"
Cohesion: 0.07
Nodes (7): Application, App, MainWindowViewModelTests, MainWindowViewModel, CurrentPageIdentifier, CurrentPageViewModel, NavigateCommand

### Community 7 - "MeasurementViewModel"
Cohesion: 0.06
Nodes (25): MeasurementViewModel, ApplyMeasurementTimeCommand, ApplySerialNumberCommand, ClearFieldsCommand, ClearLogCommand, CurrentDevice, DeviceCalibrationStatusText, DeviceConfigurationTree (+17 more)

### Community 8 - "DeviceUnderTest"
Cohesion: 0.14
Nodes (10): DeviceUnderTest, DeviceConfiguration, IsTV, MeasurementDateTime, Measurements, SerialNumber, MeasurementJsonConverterTests, TestObjectWithMeasurement (+2 more)

### Community 9 - "LogHandlerTests"
Cohesion: 0.14
Nodes (3): LogHandler, UploadService, LogHandlerTests

### Community 10 - "RelayCommand"
Cohesion: 0.16
Nodes (3): WPF_LCD_Test.UnitTests.Commands, RelayCommand, RelayCommandTests

### Community 11 - "SettingsViewModel"
Cohesion: 0.12
Nodes (8): SettingsViewModel, AutoConnectEnabled, AvailableLanguages, CancelSettingsCommand, ColorAnalyzerChannel, LanguageCultureCode, SaveSettingsCommand, SelectedLanguage

### Community 12 - "release_manager.py"
Cohesion: 0.13
Nodes (6): ensure_remote_dir(), get_nc_client(), get_sha256(), main(), prepare_package(), upload_chunked()

### Community 13 - "JsonSerializerOptions"
Cohesion: 0.13
Nodes (5): CustomDateTimeConverter, MeasurementJsonConverter, CustomDateTimeConverterTests, TestObjectWithDateTime, TestDateTime

### Community 14 - "SettingsViewModelTests"
Cohesion: 0.10
Nodes (4): SettingsViewModelTests, LanguageOption, CultureCode, DisplayName

### Community 15 - "SettingsService"
Cohesion: 0.16
Nodes (7): SettingsService, CurrentSettings, SettingsServiceTests, TestPathProvider, BaseDirectory, ConfigDirectory, DataDirectory

### Community 18 - "IColorAnalyzer"
Cohesion: 0.12
Nodes (19): IColorAnalyzer, IColorAnalyzer200, SingleCa, AveragingMode, DisplayMode, ID, Memory, Number (+11 more)

### Community 19 - "App.ConfigureServices"
Cohesion: 0.14
Nodes (14): [1.2.1] - 2026-01-26, Added, Changed, Architecture, WPF_LCD_Test, AppConstants, IPathProvider, BaseDirectory (+6 more)

### Community 20 - "Screen Checker.UnitTests.csproj"
Cohesion: 0.11
Nodes (16): net8.0-windows, net8.0-windows7.0, coverlet.collector (6.0.0), MaterialDesignThemes (5.2.2-ci976), Microsoft.NET.Test.Sdk (17.8.0), Moq (4.20.72), NUnit (3.14.0), NUnit3TestAdapter (4.5.0) (+8 more)

### Community 21 - "Window"
Cohesion: 0.18
Nodes (10): CurrentPageViewModel, IsChecked, NavigateCommand, Title, CloseImage, MaximizeRestoreImage, MinimizeImage, Window (+2 more)

### Community 22 - "DialogService"
Cohesion: 0.20
Nodes (4): IMessageBox, DialogService, DialogServiceTests, MessageBoxWrapper

### Community 23 - "ColorAnalyzerWrapper"
Cohesion: 0.13
Nodes (8): SingleCa, ColorAnalyzerWrapper, AveragingMode, DisplayMode, ID, Number, PortID, SyncMode

### Community 24 - "MeasurementView"
Cohesion: 0.16
Nodes (3): DeviceConfigurationButton, MeasurementView, Button

### Community 25 - "AppSettings"
Cohesion: 0.16
Nodes (5): AppSettings, AutoConnectEnabled, ColorAnalyzerChannel, LanguageCultureCode, AppSettingsTests

### Community 26 - "Changelog"
Cohesion: 0.13
Nodes (15): [1.0.0] - 2025-04-28, [1.2.0] - 2025-11-19, [1.2.2] - 2026-01-28, [1.2.3] - 2026-02-03, [1.2.4] - 2026-05-07, [1.3.0] - 2026-08-11, Added, Added (+7 more)

### Community 27 - "ColorMeasurementService"
Cohesion: 0.22
Nodes (5): ColorMeasurementService, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, ProbeSN

### Community 28 - "UserControl"
Cohesion: 0.15
Nodes (10): AvailableLanguages, CancelSettingsCommand, ColorAnalyzerChannel, LanguageCultureCode, SaveSettingsCommand, SelectedLanguage, ColorAnalyzerChannelTextBox, UserControl (+2 more)

### Community 29 - "DeviceConfigTreeNode"
Cohesion: 0.20
Nodes (7): DeviceConfigTreeNode, Children, DisplayName, IsFolder, IsSelected, RelativePath, SelectCommand

### Community 30 - "CLAUDE.md"
Cohesion: 0.18
Nodes (7): Build & Test Commands, Communication Style & Code Standards, Configuration, Localization, Project Overview, Testing, Version

### Community 34 - "ISettingsService"
Cohesion: 0.22
Nodes (6): ISettingsService, CurrentSettings, MockSettingsViewModel, IsDisposed, LocalizationService, SettingsService

### Community 35 - "ColorAnalyzerMemoryWrapper"
Cohesion: 0.25
Nodes (7): Dependency Injection, Key Components, Services, Wrappers, ColorAnalyzerMemoryWrapper, ChannelNO, Memory

### Community 36 - "ColorAnalyzerProbeWrapper"
Cohesion: 0.25
Nodes (7): ColorAnalyzerProbeWrapper, Lv, SerialNO, sx, sy, T, SingleProbe

### Community 37 - "MeasurementViewModel.UpdateUIState"
Cohesion: 0.22
Nodes (6): IsDeviceCalibrated, IsDeviceConnected, IsReportGenerating, IsSerialNumberConfirmed, IsUploadingReports, SerialNumber

### Community 38 - "IColorMeasurementService"
Cohesion: 0.25
Nodes (5): IColorMeasurementService, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, ProbeSN

### Community 39 - "Screen Checker"
Cohesion: 0.25
Nodes (8): Architecture, Changelog, License, Pattern: MVVM (Model-View-ViewModel), Planned Features, Roadmap, Screen Checker, Table of Contents

### Community 41 - "main_tests"
Cohesion: 0.33
Nodes (5): main_tests, White_x, max, min, typ

### Community 42 - "SerialNumberTextBox"
Cohesion: 0.40
Nodes (5): LogText, SerialNumber, LogTextBox, SerialNumberTextBox, TextBox

### Community 43 - "Features"
Cohesion: 0.40
Nodes (5): Data Management, Device Support, Features, Measurement Capabilities, User Interface

### Community 44 - "Testing"
Cohesion: 0.40
Nodes (5): Running All Tests, Running Single Test Method, Running Specific Test Class, Test Coverage, Testing

### Community 45 - "ReportExitCode"
Cohesion: 0.40
Nodes (5): ReportExitCode, ConfigError, GeneralError, NoDataFound, Success

### Community 46 - "[1.1.0] - 2025-10-15"
Cohesion: 0.50
Nodes (4): [1.1.0] - 2025-10-15, Added, Changed, ColorAnalyzer

### Community 47 - "Releasing"
Cohesion: 0.50
Nodes (4): 1. Configure credentials, 2. Publish the application, 3. Run the release manager, Releasing

### Community 48 - "Development"
Cohesion: 0.50
Nodes (4): Adding New Device Configurations, Coding Conventions, Development, Project Structure

### Community 49 - "Localization"
Cohesion: 0.50
Nodes (4): Adding New Languages, Language Switching, Localization, Supported Languages

### Community 50 - "Building the Project"
Cohesion: 0.50
Nodes (4): Build Output, Building the Project, Using MSBuild (Recommended), Using Visual Studio

### Community 51 - "Usage"
Cohesion: 0.50
Nodes (4): Configuration Files, Initial Setup, Performing Measurements, Usage

### Community 53 - "Blue_x"
Cohesion: 0.50
Nodes (4): max, min, typ, Blue_x

### Community 54 - "Blue_y"
Cohesion: 0.50
Nodes (4): max, min, typ, Blue_y

### Community 55 - "Brightness"
Cohesion: 0.50
Nodes (4): max, min, typ, Brightness

### Community 56 - "Brightness_uniformity"
Cohesion: 0.50
Nodes (4): max, min, typ, Brightness_uniformity

### Community 57 - "Cg_ntsc_area"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_ntsc_area

### Community 58 - "Cg_ntsc"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_ntsc

### Community 59 - "Cg_rgb_area"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_rgb_area

### Community 60 - "Cg_rgb"
Cohesion: 0.50
Nodes (4): max, min, typ, Cg_rgb

### Community 61 - "Contrast"
Cohesion: 0.50
Nodes (4): max, min, typ, Contrast

### Community 62 - "Delta_e"
Cohesion: 0.50
Nodes (4): max, min, typ, Delta_e

### Community 63 - "Green_x"
Cohesion: 0.50
Nodes (4): max, min, typ, Green_x

### Community 64 - "Green_y"
Cohesion: 0.50
Nodes (4): max, min, typ, Green_y

### Community 65 - "Red_x"
Cohesion: 0.50
Nodes (4): Red_x, max, min, typ

### Community 66 - "Red_y"
Cohesion: 0.50
Nodes (4): Red_y, max, min, typ

### Community 67 - "Temperature"
Cohesion: 0.50
Nodes (4): Temperature, max, min, typ

### Community 68 - "White_y"
Cohesion: 0.50
Nodes (4): White_y, max, min, typ

### Community 69 - "PART_Popup"
Cohesion: 0.67
Nodes (3): IsSubmenuOpen, PART_Popup, Popup

### Community 72 - "Configuration"
Cohesion: 0.67
Nodes (3): Application Settings, Configuration, Device Configurations

### Community 73 - "System Requirements"
Cohesion: 0.67
Nodes (3): Development Requirements, Runtime Requirements, System Requirements

### Community 74 - "Installation"
Cohesion: 0.67
Nodes (3): For Developers, For End Users, Installation

### Community 75 - "Running the Application"
Cohesion: 0.67
Nodes (3): From Build Output, From Visual Studio, Running the Application

### Community 77 - "Bg"
Cohesion: 0.67
Nodes (3): Bg, Underline, Border

## Knowledge Gaps
- **372 isolated node(s):** `SingleCa`, `SingleProbe`, `Memory`, `Number`, `ID` (+367 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 552 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **14 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MeasurementViewModel` connect `MeasurementViewModel` to `Test`, `WPF_LCD_Test.Interfaces`, `MeasurementStatus`, `FileService`, `UserControl`, `MainWindowViewModelTests`, `DeviceUnderTest`, `LogHandlerTests`, `JsonSerializerOptions`, `MeasurementViewModel.Log`, `App.ConfigureServices`, `DeviceConfigTreeNode`, `IDispatcher`, `MeasurementViewModel.ExecuteThreadInUI`, `ISettingsService`, `MeasurementViewModel.UpdateUIState`, `IColorMeasurementService`, `MeasurementViewModel.ExecuteZeroCalibrationAsync`, `ReportExitCode`, `MeasurementViewModel.SerialNumberRegex`, `IUploadService`?**
  _High betweenness centrality (0.275) - this node is a cross-community bridge._
- **What connects `SingleCa`, `SingleProbe`, `Memory` to the rest of the system?**
  _372 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Resources` be split into smaller, more focused modules?**
  _Cohesion score 0.0136986301369863 - nodes in this community are weakly interconnected._
- **Why does `Resources` connect `Resources` to `FileService`, `Resources.Designer.cs`?**
  _High betweenness centrality (0.188) - this node is a cross-community bridge._
- **Should `Test` be split into smaller, more focused modules?**
  _Cohesion score 0.05674931129476584 - nodes in this community are weakly interconnected._
- **Why does `ILocalizationService` connect `FileService` to `Test`, `WPF_LCD_Test.Interfaces`, `ISettingsService`, `MainWindowViewModelTests`, `MeasurementViewModel`, `LogHandlerTests`, `SettingsViewModel`, `SettingsViewModelTests`, `App.ConfigureServices`, `Changelog`?**
  _High betweenness centrality (0.157) - this node is a cross-community bridge._
- **Should `WPF_LCD_Test.Interfaces` be split into smaller, more focused modules?**
  _Cohesion score 0.05733397037744864 - nodes in this community are weakly interconnected._