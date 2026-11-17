# Graph Report - WPF-LCD-Test  (2026-11-17)

## Corpus Check
- 87 files · ~43,628 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 9 file(s) not represented in the graph (top: (none) 2, .bmp 2, .resx 2)

## Summary
- 1508 nodes · 3071 edges · 92 communities (73 shown, 19 thin omitted)
- Extraction: 85% EXTRACTED · 15% INFERRED · 0% AMBIGUOUS · INFERRED: 459 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `712927b5`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Resources
- WPF_LCD_Test.Services
- WPF_LCD_Test.Models
- MeasurementStatus
- AnalyzerRegistryService
- UserControl
- App.ConfigureServices
- MeasurementViewModel
- Measurement
- LogHandlerTests
- RelayCommand
- SettingsViewModel
- release_manager.py
- Ca310ProbeWrapper
- SettingsViewModelTests
- SettingsService
- IDialogService
- ColorMeasurementService
- IColorAnalyzer
- App.ConfigureServices
- Screen Checker.UnitTests.csproj
- Window
- DialogService
- Ca310Wrapper
- Ca410AnalyzerWrapper
- AppSettings
- Changelog
- Ca410Wrapper
- system_text_json
- MainWindowViewModel
- WPF_LCD_Test.Interfaces
- Ca410AnalyzerWrapper
- Ca410ProbeWrapper
- MockMeasurementViewModel
- MeasurementViewModel.cs
- Test
- MeasurementViewModel.UpdateUIState
- MeasurementView
- Screen Checker
- App.xaml.cs
- main_tests
- SerialNumberTextBox
- UploadReportItem
- ReportExitCode
- FileService
- MeasurementJsonConverterTests
- MeasurementView
- UploadService
- Action
- Features
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
- Testing
- Releasing
- Development
- Localization
- Building the Project
- Bg
- StringResources.xaml
- StringResources.zh-Hans.xaml
- CheckGlyph
- SubmenuArrow
- CLAUDE.md
- Usage
- Configuration
- System Requirements
- Installation
- [1.1.0] - 2025-10-15
- Running the Application

## God Nodes (most connected - your core abstractions)
1. `Resources` - 154 edges
2. `MeasurementViewModel` - 123 edges
3. `MeasurementViewModelTests` - 82 edges
4. `UserControl` - 68 edges
5. `SettingsViewModel` - 42 edges
6. `WPF_LCD_Test.Models` - 42 edges
7. `WPF_LCD_Test.Interfaces` - 40 edges
8. `SettingsViewModelTests` - 35 edges
9. `MeasurementStatus` - 35 edges
10. `RelayCommand` - 33 edges

## Surprising Connections (you probably didn't know these)
- `Changed` --references--> `MeasurementStatusService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Services/MeasurementStatusService.cs
- `Added` --references--> `UploadService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Services/UploadService.cs
- `Changed` --references--> `ILocalizationService`  [INFERRED]
  CHANGELOG.md → WPF LCD Test/Interfaces/ILocalizationService.cs
- `Architecture` --references--> `RelayCommand`  [INFERRED]
  CLAUDE.md → WPF LCD Test/Commands/RelayCommand.cs
- `Added` --references--> `ColorMeasurementServiceTests`  [INFERRED]
  CHANGELOG.md → WPF_LCD_Test.UnitTests/ServicesTests/ColorMeasurementServiceTests.cs

## Import Cycles
- None detected.

## Communities (92 total, 19 thin omitted)

### Community 0 - "Resources"
Cohesion: 0.01
Nodes (151): Resources, ActiveChannelStatus, Actual, AlreadyActivated, ArchiveFolderNotFound, Attempt, BadConnection, Batches (+143 more)

### Community 1 - "WPF_LCD_Test.Services"
Cohesion: 0.15
Nodes (4): WPF_LCD_Test.UnitTests.ViewModelsTests, WPF_LCD_Test.Services, WPF_LCD_Test.UnitTests.ServicesTests, WPF_LCD_Test.Tests

### Community 3 - "MeasurementStatus"
Cohesion: 0.05
Nodes (29): IMeasurementStatusService, AllMeasurementButtonStatuses, Statuses, MeasurementLocation, BlackColor, BlueColor, BottomCenter, BottomLeft (+21 more)

### Community 4 - "AnalyzerRegistryService"
Cohesion: 0.08
Nodes (14): CustomDateTimeConverter, MeasurementJsonConverter, IAnalyzerRegistryService, AnalyzerRegistryEntry, LastChannel, LastSeenUtc, Model, ProbeSN (+6 more)

### Community 5 - "UserControl"
Cohesion: 0.04
Nodes (56): Children, ClearLogCommand, BooleanToVisibilityConverter, DataContext.ApplySerialNumberCommand, DataContext.ClearFieldsCommand, DataContext.MeasureCommand, DataContext.MeasurementStatusService.Statuses[BlackColor].MeasuredValuesString, DataContext.MeasurementStatusService.Statuses[BlackColor].StatusColor (+48 more)

### Community 6 - "App.ConfigureServices"
Cohesion: 0.07
Nodes (3): Application, App, MainWindowViewModelTests

### Community 7 - "MeasurementViewModel"
Cohesion: 0.05
Nodes (29): MeasurementViewModel, ApplyMeasurementTimeCommand, ApplySerialNumberCommand, ClearFieldsCommand, ClearLogCommand, CurrentDevice, DeviceActiveChannelStatusText, DeviceCalibrationStatusText (+21 more)

### Community 8 - "Measurement"
Cohesion: 0.10
Nodes (17): ColorimeterInfo, DeviceUnderTest, Colorimeter, DeviceConfiguration, IsTV, MeasurementDateTime, Measurements, SerialNumber (+9 more)

### Community 9 - "LogHandlerTests"
Cohesion: 0.08
Nodes (10): ILocalizationService, CurrentCulture, LogHandler, LocalizationService, CurrentCulture, LogHandlerTests, MockLocalizationService, CurrentCulture (+2 more)

### Community 10 - "RelayCommand"
Cohesion: 0.17
Nodes (3): WPF_LCD_Test.UnitTests.Commands, RelayCommand, RelayCommandTests

### Community 11 - "SettingsViewModel"
Cohesion: 0.12
Nodes (8): SettingsViewModel, AutoConnectEnabled, AvailableLanguages, CancelSettingsCommand, ColorAnalyzerChannel, LanguageCultureCode, SaveSettingsCommand, SelectedLanguage

### Community 12 - "release_manager.py"
Cohesion: 0.13
Nodes (6): ensure_remote_dir(), get_nc_client(), get_sha256(), main(), prepare_package(), upload_chunked()

### Community 13 - "Ca310ProbeWrapper"
Cohesion: 0.12
Nodes (15): IColorAnalyzerProbe, Lv, SerialNO, sx, sy, T, TypeName, Ca310ProbeWrapper (+7 more)

### Community 14 - "SettingsViewModelTests"
Cohesion: 0.10
Nodes (4): SettingsViewModelTests, LanguageOption, CultureCode, DisplayName

### Community 15 - "SettingsService"
Cohesion: 0.17
Nodes (7): SettingsService, CurrentSettings, SettingsServiceTests, TestPathProvider, BaseDirectory, ConfigDirectory, DataDirectory

### Community 17 - "ColorMeasurementService"
Cohesion: 0.14
Nodes (6): ColorMeasurementService, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, Model, ProbeSN

### Community 18 - "IColorAnalyzer"
Cohesion: 0.12
Nodes (9): IColorAnalyzer, AveragingMode, DisplayMode, ID, Memory, Number, PortID, SingleProbe (+1 more)

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
Cohesion: 0.18
Nodes (4): IMessageBox, DialogService, DialogServiceTests, MessageBoxWrapper

### Community 23 - "Ca310Wrapper"
Cohesion: 0.13
Nodes (13): IColorAnalyzerMemory, ChannelNO, SingleCa, Ca310MemoryWrapper, ChannelNO, Ca310Wrapper, AveragingMode, DisplayMode (+5 more)

### Community 24 - "Ca410AnalyzerWrapper"
Cohesion: 0.14
Nodes (8): Dependency Injection, Key Components, Services, Wrappers, Ca410AnalyzerWrapper, Model, Root, SingleCa

### Community 25 - "AppSettings"
Cohesion: 0.15
Nodes (7): ISettingsService, CurrentSettings, AppSettings, AutoConnectEnabled, ColorAnalyzerChannel, LanguageCultureCode, AppSettingsTests

### Community 26 - "Changelog"
Cohesion: 0.13
Nodes (15): [1.0.0] - 2025-04-28, [1.2.0] - 2025-11-19, [1.2.2] - 2026-01-28, [1.2.3] - 2026-02-03, [1.2.4] - 2026-05-07, [1.3.0] - 2026-08-11, Added, Added (+7 more)

### Community 27 - "Ca410Wrapper"
Cohesion: 0.14
Nodes (14): Ca410MemoryWrapper, ChannelNO, Memory, Ca410Sdk, Ca410Wrapper, AveragingMode, Ca, DisplayMode (+6 more)

### Community 29 - "MainWindowViewModel"
Cohesion: 0.10
Nodes (15): DeviceConfigTreeNode, Children, DisplayName, IsFolder, IsSelected, RelativePath, SelectCommand, MockSettingsViewModel (+7 more)

### Community 32 - "Ca410AnalyzerWrapper"
Cohesion: 0.13
Nodes (10): IColorAnalyzer200, Model, SingleCa, ColorAnalyzerModel, CA310, CA410, Unknown, ColorAnalyzerModelExtensions (+2 more)

### Community 33 - "Ca410ProbeWrapper"
Cohesion: 0.16
Nodes (10): Ca410ProbeWrapper, Lv, Probe, SerialNO, sx, sy, T, TypeName (+2 more)

### Community 34 - "MockMeasurementViewModel"
Cohesion: 0.14
Nodes (12): IColorMeasurementService, CurrentChannel, IsDeviceCalibrated, IsDeviceConnected, Model, ProbeSN, MockMeasurementViewModel, ColorMeasurementService (+4 more)

### Community 35 - "MeasurementViewModel.cs"
Cohesion: 0.18
Nodes (4): WPF_LCD_Test.Commands, WPF_LCD_Test.ViewModels, WPF_LCD_Test.UnitTests.Mocks, WPF_LCD_Test.UnitTests.ViewModels

### Community 36 - "Test"
Cohesion: 0.05
Nodes (4): ColorMeasurementServiceTests, FileServiceTests, UploadServiceTests, MeasurementViewModelTests

### Community 37 - "MeasurementViewModel.UpdateUIState"
Cohesion: 0.16
Nodes (6): IsDeviceCalibrated, IsDeviceConnected, IsReportGenerating, IsSerialNumberConfirmed, IsUploadingReports, SerialNumber

### Community 38 - "MeasurementView"
Cohesion: 0.06
Nodes (19): AvailableLanguages, CancelSettingsCommand, ColorAnalyzerChannel, LanguageCultureCode, SaveSettingsCommand, SelectedLanguage, FirstButton, MessageText (+11 more)

### Community 39 - "Screen Checker"
Cohesion: 0.25
Nodes (8): Architecture, Changelog, License, Pattern: MVVM (Model-View-ViewModel), Planned Features, Roadmap, Screen Checker, Table of Contents

### Community 41 - "main_tests"
Cohesion: 0.33
Nodes (5): main_tests, White_x, max, min, typ

### Community 42 - "SerialNumberTextBox"
Cohesion: 0.33
Nodes (5): LogText, SerialNumber, LogTextBox, SerialNumberTextBox, TextBox

### Community 43 - "UploadReportItem"
Cohesion: 0.24
Nodes (4): UploadReportItem, LocalFilesToUpload, ReportRemoteDirectory, UploadReportItemTests

### Community 45 - "ReportExitCode"
Cohesion: 0.40
Nodes (5): ReportExitCode, ConfigError, GeneralError, NoDataFound, Success

### Community 46 - "FileService"
Cohesion: 0.19
Nodes (6): IFileService, BaseFolderPath, WorkFolderName, FileService, BaseFolderPath, WorkFolderName

### Community 48 - "MeasurementJsonConverterTests"
Cohesion: 0.27
Nodes (3): MeasurementJsonConverterTests, TestObjectWithMeasurement, TestMeasurement

### Community 49 - "MeasurementView"
Cohesion: 0.29
Nodes (3): MeasurementView, DeviceConfigurationButton, Button

### Community 52 - "Features"
Cohesion: 0.40
Nodes (5): Data Management, Device Support, Features, Measurement Capabilities, User Interface

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

### Community 72 - "Testing"
Cohesion: 0.40
Nodes (5): Running All Tests, Running Single Test Method, Running Specific Test Class, Test Coverage, Testing

### Community 73 - "Releasing"
Cohesion: 0.50
Nodes (4): 1. Configure credentials, 2. Publish the application, 3. Run the release manager, Releasing

### Community 74 - "Development"
Cohesion: 0.50
Nodes (4): Adding New Device Configurations, Coding Conventions, Development, Project Structure

### Community 75 - "Localization"
Cohesion: 0.50
Nodes (4): Adding New Languages, Language Switching, Localization, Supported Languages

### Community 76 - "Building the Project"
Cohesion: 0.50
Nodes (4): Build Output, Building the Project, Using MSBuild (Recommended), Using Visual Studio

### Community 77 - "Bg"
Cohesion: 0.67
Nodes (3): Bg, Underline, Border

### Community 83 - "CLAUDE.md"
Cohesion: 0.17
Nodes (8): Build & Test Commands, Communication Style & Code Standards, Configuration, Localization, Project Map (graphify), Project Overview, Testing, Version

### Community 85 - "Usage"
Cohesion: 0.50
Nodes (4): Configuration Files, Initial Setup, Performing Measurements, Usage

### Community 86 - "Configuration"
Cohesion: 0.67
Nodes (3): Application Settings, Configuration, Device Configurations

### Community 87 - "System Requirements"
Cohesion: 0.67
Nodes (3): Development Requirements, Runtime Requirements, System Requirements

### Community 88 - "Installation"
Cohesion: 0.67
Nodes (3): For Developers, For End Users, Installation

### Community 89 - "[1.1.0] - 2025-10-15"
Cohesion: 0.50
Nodes (4): [1.1.0] - 2025-10-15, Added, Changed, ColorAnalyzer

### Community 90 - "Running the Application"
Cohesion: 0.67
Nodes (3): From Build Output, From Visual Studio, Running the Application

## Knowledge Gaps
- **409 isolated node(s):** `TestMeasurement`, `MeasurementViewModel`, `Children`, `DisplayName`, `TextBlock` (+404 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 613 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **19 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Resources` connect `Resources` to `LogHandlerTests`, `Resources.Designer.cs`?**
  _High betweenness centrality (0.174) - this node is a cross-community bridge._
- **What connects `TestMeasurement`, `MeasurementViewModel`, `Children` to the rest of the system?**
  _409 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Resources` be split into smaller, more focused modules?**
  _Cohesion score 0.013071895424836602 - nodes in this community are weakly interconnected._
- **Why does `MeasurementViewModel` connect `MeasurementViewModel` to `Ca410AnalyzerWrapper`, `MeasurementStatus`, `Test`, `AnalyzerRegistryService`, `App.ConfigureServices`, `MeasurementViewModel.UpdateUIState`, `MeasurementViewModel.SerialNumberRegex`, `UploadService.cs`, `ReportExitCode`, `MeasurementViewModelTests.Setup`, `SettingsService`, `IDialogService`, `MeasurementViewModel.ProcessValidMeasurement`, `MeasurementViewModel.Log`, `MainWindowViewModel`, `MeasurementViewModel.ExecuteThreadInUI`?**
  _High betweenness centrality (0.129) - this node is a cross-community bridge._
- **Should `MeasurementStatus` be split into smaller, more focused modules?**
  _Cohesion score 0.05271629778672032 - nodes in this community are weakly interconnected._
- **Why does `ILocalizationService` connect `LogHandlerTests` to `WPF_LCD_Test.Services`, `MockMeasurementViewModel`, `Test`, `SettingsViewModel`, `FileService`, `SettingsViewModelTests`, `Changelog`, `MainWindowViewModel`?**
  _High betweenness centrality (0.121) - this node is a cross-community bridge._
- **Should `AnalyzerRegistryService` be split into smaller, more focused modules?**
  _Cohesion score 0.07712765957446809 - nodes in this community are weakly interconnected._