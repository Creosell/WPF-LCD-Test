# WPF LCD Test

Desktop application for testing color characteristics of LCD displays using Konica Minolta CA-200/CA-310 colorimeters.

## Technologies

- .NET 8.0
- WPF (Windows Presentation Foundation)
- Material Design Themes
- MVVM architecture

## Features

- Konica Minolta colorimeter connection and calibration
- Color coordinate measurement (x, y), brightness, and color temperature
- Multi-channel measurement support (0-99)
- Device configuration loading from YAML files
- Measurement results export
- Parallel report upload to server
- Localization (English, 中文)

## Build and Run

Requires Visual Studio 2022+ with .NET 8.0 SDK installed.

```bash
msbuild "WPF LCD Test.sln" /p:Configuration=Release
```

## Testing

```bash
dotnet test "WPF_LCD_Test.UnitTests\WPF_LCD_Test.UnitTests.csproj"
```

## Planned Features

- Counter for DUT (Device Under Test)
- Page for working with results in folder
- Split location parameter into MeasurementPoint with two parameters: Position and Color
- Move measurement time to settings
