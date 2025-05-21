// В проекте WPF_LCD_Test.UnitTests (папка ModelsTests)
// Файл MeasurementStatusViewModelTests.cs

using NUnit.Framework;
using WPF_LCD_Test.Models; // Для MeasurementStatusViewModel
using System.Windows.Media; // Для Brush
using System.ComponentModel; // Для INotifyPropertyChanged

namespace WPF_LCD_Test.UnitTests.ModelsTests
{
    [TestFixture]
    public class MeasurementStatusViewModelTests
    {
        // Тест для конструктора с параметром location
        [Test]
        public void Constructor_WithLocation_InitializesCorrectly()
        {
            // Arrange
            string location = "TestLocation";

            // Act
            MeasurementStatusViewModel vm = new MeasurementStatusViewModel(location);

            // Assert
            Assert.That(vm.Location, Is.EqualTo(location), "Location should be initialized correctly.");
            Assert.That(vm.IsPassed, Is.Null, "IsPassed should be null by default.");
            Assert.That(vm.MeasuredValuesString, Is.EqualTo(""), "MeasuredValuesString should be empty by default.");
            Assert.That(vm.StatusColor, Is.EqualTo(Brushes.DimGray), "StatusColor should be DimGray when IsPassed is null.");
        }

        // Тест для конструктора по умолчанию
        [Test]
        public void DefaultConstructor_InitializesCorrectly()
        {
            // Act
            MeasurementStatusViewModel vm = new MeasurementStatusViewModel();

            // Assert
            Assert.That(vm.Location, Is.EqualTo("Unknown"), "Location should be 'Unknown' by default.");
            Assert.That(vm.IsPassed, Is.Null, "IsPassed should be null by default.");
            Assert.That(vm.MeasuredValuesString, Is.EqualTo(""), "MeasuredValuesString should be empty by default.");
            Assert.That(vm.StatusColor, Is.EqualTo(Brushes.DimGray), "StatusColor should be DimGray when IsPassed is null.");
        }

        // Тест для свойства Location (базовая проверка)
        [Test]
        public void Location_CanBeSetAndGet()
        {
            // Arrange
            MeasurementStatusViewModel vm = new MeasurementStatusViewModel();
            string newLocation = "NewTestLocation";

            // Act
            vm.Location = newLocation;

            // Assert
            Assert.That(vm.Location, Is.EqualTo(newLocation));
        }

        // Тесты для свойства IsPassed и связанного StatusColor
        [TestCase(true, typeof(SolidColorBrush), "DarkGreen")] // Успех
        [TestCase(false, typeof(SolidColorBrush), "DarkRed")]  // Ошибка
        [TestCase(null, typeof(SolidColorBrush), "DimGray")]   // Не измерено / По умолчанию
        public void StatusColor_ReturnsCorrectBrushBasedOnIsPassed(bool? isPassedValue, Type expectedBrushType, string expectedColorName)
        {
            // Arrange
            MeasurementStatusViewModel vm = new MeasurementStatusViewModel();

            // Act
            vm.IsPassed = isPassedValue;

            // Assert
            Assert.That(vm.StatusColor, Is.InstanceOf(expectedBrushType), $"StatusColor should be of type {expectedBrushType.Name}.");
            SolidColorBrush brush = (SolidColorBrush)vm.StatusColor;

            // Проверяем цвет по имени. ConvertFromString не всегда работает, но для стандартных цветов Brushes это надежно.
            Color expectedColor = (Color)ColorConverter.ConvertFromString(expectedColorName);
            Assert.That(brush.Color, Is.EqualTo(expectedColor), $"StatusColor should be {expectedColorName}.");
        }

        // Тест для IsPassed: Проверка вызова PropertyChanged для IsPassed и StatusColor
        [Test]
        public void IsPassed_SetProperty_RaisesPropertyChangedForIsPassedAndStatusColor()
        {
            // Arrange
            MeasurementStatusViewModel vm = new MeasurementStatusViewModel(); // IsPassed is initially null
            int isPassedChangedCount = 0;
            int statusColorChangedCount = 0;

            ((INotifyPropertyChanged)vm).PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MeasurementStatusViewModel.IsPassed))
                {
                    isPassedChangedCount++;
                }
                else if (e.PropertyName == nameof(MeasurementStatusViewModel.StatusColor))
                {
                    statusColorChangedCount++;
                }
            };

            // Act 1: Change IsPassed from null to true
            vm.IsPassed = true;

            // Assert 1
            Assert.That(isPassedChangedCount, Is.EqualTo(1), "IsPassed should raise PropertyChanged once after first change.");
            Assert.That(statusColorChangedCount, Is.EqualTo(1), "StatusColor should raise PropertyChanged once after IsPassed changes.");
            Assert.That(vm.IsPassed, Is.True);
            Assert.That(vm.StatusColor, Is.EqualTo(Brushes.DarkGreen));

            // Act 2: Change IsPassed from true to false
            vm.IsPassed = false;

            // Assert 2
            Assert.That(isPassedChangedCount, Is.EqualTo(2), "IsPassed should raise PropertyChanged again after second change.");
            Assert.That(statusColorChangedCount, Is.EqualTo(2), "StatusColor should raise PropertyChanged again after IsPassed changes.");
            Assert.That(vm.IsPassed, Is.False);
            Assert.That(vm.StatusColor, Is.EqualTo(Brushes.DarkRed));

            // Act 3: Set IsPassed to the same value (false) - should NOT raise PropertyChanged
            vm.IsPassed = false;

            // Assert 3
            Assert.That(isPassedChangedCount, Is.EqualTo(2), "IsPassed should NOT raise PropertyChanged when setting to the same value.");
            Assert.That(statusColorChangedCount, Is.EqualTo(2), "StatusColor should NOT raise PropertyChanged when IsPassed is set to the same value.");
        }

        // Тест для MeasuredValuesString: Проверка вызова PropertyChanged
        [Test]
        public void MeasuredValuesString_SetProperty_RaisesPropertyChanged()
        {
            // Arrange
            MeasurementStatusViewModel vm = new MeasurementStatusViewModel();
            int changedCount = 0;

            ((INotifyPropertyChanged)vm).PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MeasurementStatusViewModel.MeasuredValuesString))
                {
                    changedCount++;
                }
            };

            // Act 1: Set a new value
            vm.MeasuredValuesString = "Test Value 1";

            // Assert 1
            Assert.That(changedCount, Is.EqualTo(1), "MeasuredValuesString should raise PropertyChanged once.");
            Assert.That(vm.MeasuredValuesString, Is.EqualTo("Test Value 1"));

            // Act 2: Set another new value
            vm.MeasuredValuesString = "Test Value 2";

            // Assert 2
            Assert.That(changedCount, Is.EqualTo(2), "MeasuredValuesString should raise PropertyChanged again.");
            Assert.That(vm.MeasuredValuesString, Is.EqualTo("Test Value 2"));

            // Act 3: Set to the same value - should NOT raise PropertyChanged
            vm.MeasuredValuesString = "Test Value 2";

            // Assert 3
            Assert.That(changedCount, Is.EqualTo(2), "MeasuredValuesString should NOT raise PropertyChanged when setting to the same value.");
        }
    }
}