# WPF LCD Test

Десктопное приложение для тестирования цветовых характеристик LCD-дисплеев с использованием колориметров Konica Minolta CA-200/CA-310.

## Технологии

- .NET 8.0
- WPF (Windows Presentation Foundation)
- Material Design Themes
- MVVM архитектура

## Возможности

- Подключение и калибровка колориметра Konica Minolta
- Измерение цветовых координат (x, y), яркости и цветовой температуры
- Поддержка нескольких каналов измерения (0-99)
- Загрузка конфигураций устройств из YAML-файлов
- Экспорт результатов измерений
- Параллельная загрузка отчётов на сервер
- Локализация (English, 中文)

## Сборка и запуск

Требуется Visual Studio 2022+ с установленным .NET 8.0 SDK.

```bash
msbuild "WPF LCD Test.sln" /p:Configuration=Release
```

## Тестирование

```bash
dotnet test "WPF_LCD_Test.UnitTests\WPF_LCD_Test.UnitTests.csproj"
```

## Что планируется добавить

- Счётчик для DUT (Device Under Test)
- Страница для работы с результатами в папке
- Разделение параметра location на MeasurementPoint с двумя параметрами: Position и Color
- Перенос времени измерения в настройки
