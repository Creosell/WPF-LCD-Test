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

**Entry Point:** `App.xaml.cs` configures `Microsoft.Extensions.DependencyInjection` container and resolves `MainWindowViewModel` as the application root.

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
| IFileService | File I/O abstraction, JSON/CSV export |
| IUploadService | Parallel report upload to Nextcloud |
| IDialogService | File dialogs, message boxes |
| IMeasurementStatusService | Measurement state tracking |
| IPathProvider | Centralized path resolution (BaseDirectory, ConfigDirectory, DataDirectory) |

**Testability:** All system dependencies (file system, COM device, MessageBox, WPF Dispatcher) are wrapped in interfaces injected via DI. File system uses `System.IO.Abstractions` (`IFileSystem` / `MockFileSystem`). COM device wrapped in `ColorAnalyzerMemoryWrapper` / `ColorAnalyzerProbeWrapper` with proper `Marshal.ReleaseComObject` cleanup.

**Dependency Injection:** `Microsoft.Extensions.DependencyInjection` container configured in `App.xaml.cs` → `ConfigureServices()`. Singletons: settings, localization, path provider, measurement status. Transients: ViewModels. All services use constructor injection — no static singletons.

**Path Management:** `IPathProvider` / `PathProvider` — single source of truth for `BaseDirectory`, `ConfigDirectory`, `DataDirectory`. No hardcoded `AppDomain.CurrentDomain.BaseDirectory`.

**Constants:** `AppConstants` class. `ColorAnalyzer.MinChannel` = 0, `MaxChannel` = 99. `MeasurementViewModel.COLOR_COORDINATES_TOLERANCE` = 0.15, `QA_PROBE_SN` = "08954195".

**Commands:** ViewModels use `RelayCommand` (in `Commands/`) for ICommand implementation binding to UI actions.

## Configuration

Device test specifications are YAML files in `WPF LCD Test/config/device_configs/` defining min/max/typical values for brightness, contrast, color gamut (RGB/NTSC area), RGB primaries (x/y coordinates), white point, and color temperature. Each config specifies acceptable ranges under `main_tests`.

## Version

Current version: **1.3.0** (set in `WPF LCD Test\Screen Checker.csproj` — `AssemblyVersion` / `FileVersion`). See [CHANGELOG.md](CHANGELOG.md) for history.

## Localization

Supports English (default) and Chinese Simplified. Resources in `WPF LCD Test/Resources/StringResources.xaml` and `StringResources.zh-Hans.xaml`. Language can be switched at runtime.

## Testing

- Framework: NUnit 3.14 with Moq 4.20, `System.IO.Abstractions.TestingHelpers` for file system mocking
- Tests mirror source structure in `WPF_LCD_Test.UnitTests/`
- Coverage: ViewModels, all Services (including COM lifecycle), Models, Converters, Commands
- Do **not** use `dotnet build` to build before running tests — use MSBuild due to COM interop

## Communication Style & Code Standards

**Response Format:**
1. **CONCISENESS**: Minimal responses. No pleasantries ("Конечно", "Я могу помочь").
2. **FORMAT**: Use Markdown. Code blocks must specify language.
3. **SCOPE**: Return only changed code blocks with minimal context (3 lines above/below). DO NOT return full files unless explicitly requested.
4. **EXPLANATIONS**: Skip explanations for trivial changes. Use short bullet lists for complex logic.
5. **THINKING**: Do not output thinking process unless requested.

**Documentation & Comments:**
- Do not add explanatory comments from your perspective (e.g., 'I changed this because...').
- Use only final descriptive code comments.
- Every method must have detailed XML documentation in English.
- Comments must be concise, describing code behavior, without suggestions to user.

**Code Style & Logic:**
- Prefer elegant, concise, and clean code.
- Follow Separation of Concerns and Single Source of Truth principles.
- Avoid bad programming practices.

**Tone:**
- Communication in Russian, technical documentation strictly in English.
- Respond concisely. No fluff. No "Конечно", "Вот здесь". Output only code or direct answers. If explanation needed, use bullet lists.
