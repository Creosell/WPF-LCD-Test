# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WPF desktop application for LCD color measurement testing using Konica Minolta CA-200/CA-310 color analyzers. Built with .NET 8.0, WPF, and Material Design themes.

## Build & Test Commands

**Build (Visual Studio required due to COM interop):**
```
msbuild "WPF LCD Test.sln" /p:Configuration=Debug
```

**Run Tests:**
```
dotnet test "WPF_LCD_Test.UnitTests\WPF_LCD_Test.UnitTests.csproj"
```

**Run Single Test:**
```
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

**Note:** The project uses COM interop for the Konica Minolta color analyzer device. Building via `dotnet build` may fail with ResolveComReference errors - use Visual Studio or MSBuild directly.

## Architecture

**Pattern:** MVVM (Model-View-ViewModel)

**Entry Point:** `App.xaml.cs` initializes services and creates MainWindowViewModel with manual dependency injection.

**Key Layers:**

- **Views/** - XAML UI (MainWindow, MeasurementView, SettingsView)
- **ViewModels/** - Presentation logic with RelayCommand for UI binding
- **Models/** - Data classes (Measurement, DeviceUnderTest, AppSettings)
- **Services/** - Business logic, all interface-based for testability
- **Wrappers/** - Abstractions over system dependencies (COM device, file system, dispatcher)
- **Interfaces/** - Service contracts for dependency injection

**Core Services:**

| Service | Purpose |
|---------|---------|
| IColorMeasurementService | COM interop with color analyzer device |
| ISettingsService | App configuration (singleton) |
| ILocalizationService | Runtime language switching (singleton) |
| IFileService | File I/O abstraction |
| IUploadService | Report upload with parallel support |
| IDialogService | File dialogs, message boxes |

**Testability:** All system dependencies (file system, COM device, MessageBox, WPF Dispatcher) are wrapped in interfaces and injected, allowing comprehensive mocking with Moq.

**Dependency Injection:** Manual DI in `App.xaml.cs` - services instantiated at startup and injected into MainWindowViewModel constructor. Singletons (SettingsService, LocalizationService) accessed via `.Instance` property.

**Commands:** ViewModels use `RelayCommand` (in `Commands/`) for ICommand implementation binding to UI actions.

## Configuration

Device test specifications are YAML files in `WPF LCD Test/config/device_configs/` defining min/max/typical values for brightness, contrast, color gamut (RGB/NTSC area), RGB primaries (x/y coordinates), white point, and color temperature. Each config specifies acceptable ranges under `main_tests`.

## Localization

Supports English (default) and Chinese Simplified. Resources in `WPF LCD Test/Resources/StringResources.xaml` and `StringResources.zh-Hans.xaml`. Language can be switched at runtime.

## Testing

- Framework: NUnit 3.14 with Moq
- Tests mirror source structure in `WPF_LCD_Test.UnitTests/`
- All ViewModels, Services, and Models have corresponding test classes
