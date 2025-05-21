// WPF_LCD_Test.UnitTests/ServicesTests/MeasurementStatusManagerTests.cs

using NUnit.Framework;
using System.Linq;
using System.Collections.ObjectModel;
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.ViewModels;
using System.Reflection; // Обязательно для рефлексии
using System; // Обязательно для Lazy

namespace WPF_LCD_Test.UnitTests.ServicesTests
{
    [TestFixture]
    public class MeasurementStatusManagerTests
    {
        private MeasurementStatusManager _manager;

        // Метод для сброса синглтона MeasurementStatusManager с использованием рефлексии
        private void ResetMeasurementStatusManagerSingleton()
        {
            var lazyInstanceField = typeof(MeasurementStatusManager)
                .GetField("_lazyInstance", BindingFlags.NonPublic | BindingFlags.Static);

            if (lazyInstanceField == null)
            {
                // Это не должно произойти, если поле _lazyInstance существует
                Assert.Fail("Private static field '_lazyInstance' not found.");
            }

            // Находим приватный конструктор MeasurementStatusManager
            var privateCtor = typeof(MeasurementStatusManager)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(c => c.GetParameters().Length == 0); // Ищем конструктор без параметров

            if (privateCtor == null)
            {
                Assert.Fail("Private parameterless constructor not found for MeasurementStatusManager.");
            }

            // Создаем новый Lazy<MeasurementStatusManager>, используя рефлексию для вызова приватного конструктора
            var newLazyInstance = new Lazy<MeasurementStatusManager>(() =>
            {
                return (MeasurementStatusManager)privateCtor.Invoke(null);
            });

            // Устанавливаем _lazyInstance в новый Lazy<T> объект
            lazyInstanceField.SetValue(null, newLazyInstance);
        }

        [SetUp]
        public void Setup()
        {
            // !!! СБРОС СИНГЛТОНА ПЕРЕД КАЖДЫМ ТЕСТОМ !!!
            ResetMeasurementStatusManagerSingleton();

            // Получаем (новый) экземпляр синглтона.
            _manager = MeasurementStatusManager.Instance;
        }

        [TearDown]
        public void Teardown()
        {
            // Ничего особенного не нужно, так как _manager сбрасывается в Setup
        }

        [Test]
        public void Instance_ReturnsSingletonInstance()
        {
            // Act
            var instance1 = MeasurementStatusManager.Instance;
            var instance2 = MeasurementStatusManager.Instance;

            // Assert
            Assert.That(instance1, Is.SameAs(instance2), "Instance should return the same singleton object.");
            Assert.That(instance1, Is.InstanceOf<MeasurementStatusManager>(), "Instance should be of type MeasurementStatusManager.");
        }

        [Test]
        public void Constructor_InitializesAllStatuses()
        {
            // Arrange (уже сделано в Setup)
            var expectedLocations = new string[]
            {
                MeasurementStatusManager.TopLeftLocationName,
                MeasurementStatusManager.TopCenterLocationName,
                MeasurementStatusManager.TopRightLocationName,
                MeasurementStatusManager.MiddleLeftLocationName,
                MeasurementStatusManager.CenterLocationName,
                MeasurementStatusManager.MiddleRightLocationName,
                MeasurementStatusManager.BottomLeftLocationName,
                MeasurementStatusManager.BottomCenterLocationName,
                MeasurementStatusManager.BottomRightLocationName,
                MeasurementStatusManager.RedColorLocationName,
                MeasurementStatusManager.GreenColorLocationName,
                MeasurementStatusManager.BlueColorLocationName,
                MeasurementStatusManager.BlackColorLocationName
            };

            // Assert
            Assert.That(_manager.AllMeasurementButtonStatuses, Is.Not.Null);
            Assert.That(_manager.AllMeasurementButtonStatuses.Count, Is.EqualTo(expectedLocations.Length), "AllMeasurementButtonStatuses should contain the correct number of statuses.");

            foreach (var locationName in expectedLocations)
            {
                Assert.That(_manager.AllMeasurementButtonStatuses.Any(s => s.Location == locationName), Is.True, $"Status for '{locationName}' should be in the collection.");
            }
        }

        [Test]
        public void GetStatusByLocation_ReturnsCorrectStatus()
        {
            // Arrange (коллекция уже инициализирована в Setup)
            string targetLocation = MeasurementStatusManager.CenterLocationName;

            // Act
            var status = _manager.GetStatusByLocation(targetLocation);

            // Assert
            Assert.That(status, Is.Not.Null, $"Status for '{targetLocation}' should be found.");
            Assert.That(status.Location, Is.EqualTo(targetLocation), "Returned status should have the correct location name.");
        }

        [Test]
        public void GetStatusByLocation_ReturnsNullForNonExistentLocation()
        {
            // Arrange
            string nonExistentLocation = "NonExistentLocation";

            // Act
            var status = _manager.GetStatusByLocation(nonExistentLocation);

            // Assert
            Assert.That(status, Is.Null, "GetStatusByLocation should return null for a non-existent location.");
        }

        [Test]
        public void PublicProperties_AreInitializedCorrectly()
        {
            Assert.That(_manager.TopLeftStatus, Is.Not.Null);
            Assert.That(_manager.TopLeftStatus.Location, Is.EqualTo(MeasurementStatusManager.TopLeftLocationName));

            Assert.That(_manager.CenterStatus, Is.Not.Null);
            Assert.That(_manager.CenterStatus.Location, Is.EqualTo(MeasurementStatusManager.CenterLocationName));

            Assert.That(_manager.RedColorStatus, Is.Not.Null);
            Assert.That(_manager.RedColorStatus.Location, Is.EqualTo(MeasurementStatusManager.RedColorLocationName));
        }

        [Test]
        public void InitialStatus_IsPassedIsNullAndValuesAreEmpty()
        {
            // Arrange
            string targetLocation = MeasurementStatusManager.TopLeftLocationName;
            var status = _manager.GetStatusByLocation(targetLocation);

            // Assert
            Assert.That(status, Is.Not.Null);
            Assert.That(status.IsPassed, Is.Null, "IsPassed should be null initially.");
            Assert.That(status.MeasuredValuesString, Is.Empty, "MeasuredValuesString should be empty initially.");
            Assert.That(status.StatusColor, Is.EqualTo(System.Windows.Media.Brushes.DimGray), "StatusColor should be DimGray initially.");
        }

        [Test]
        public void SetIsPassed_UpdatesStatusAndColor()
        {
            // Arrange
            string targetLocation = MeasurementStatusManager.CenterLocationName;
            var status = _manager.GetStatusByLocation(targetLocation);
            Assert.That(status, Is.Not.Null);

            // Act: Set to Passed (true)
            status.IsPassed = true;

            // Assert
            Assert.That(status.IsPassed, Is.True, "IsPassed should be true.");
            Assert.That(status.StatusColor, Is.EqualTo(System.Windows.Media.Brushes.DarkGreen), "StatusColor should be DarkGreen for passed.");

            // Act: Set to Failed (false)
            status.IsPassed = false;

            // Assert
            Assert.That(status.IsPassed, Is.False, "IsPassed should be false.");
            Assert.That(status.StatusColor, Is.EqualTo(System.Windows.Media.Brushes.DarkRed), "StatusColor should be DarkRed for failed.");

            // Act: Set back to not measured (null)
            status.IsPassed = null;

            // Assert
            Assert.That(status.IsPassed, Is.Null, "IsPassed should be null again.");
            Assert.That(status.StatusColor, Is.EqualTo(System.Windows.Media.Brushes.DimGray), "StatusColor should be DimGray for not measured.");
        }

        [Test]
        public void SetMeasuredValuesString_UpdatesValue()
        {
            // Arrange
            string targetLocation = MeasurementStatusManager.BottomLeftLocationName;
            var status = _manager.GetStatusByLocation(targetLocation);
            Assert.That(status, Is.Not.Null);

            string expectedValues = "1.23, 4.56, 7.89";

            // Act
            status.MeasuredValuesString = expectedValues;

            // Assert
            Assert.That(status.MeasuredValuesString, Is.EqualTo(expectedValues), "MeasuredValuesString should be updated.");
        }
    }
}