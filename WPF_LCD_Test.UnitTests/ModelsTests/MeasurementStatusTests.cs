using System.ComponentModel;
using System.Windows.Media;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.UnitTests.ModelsTests
{
    [TestFixture]
    public class MeasurementStatusTests
    {
        [Test]
        public void Constructor_WithLocation_InitializesLocationCorrectly()
        {
            // Arrange
            string location = "Center";

            // Act
            var status = new MeasurementStatus(location);

            // Assert
            Assert.That(status.Location, Is.EqualTo(location));
            Assert.That(status.IsPassed, Is.Null);
            Assert.That(status.MeasuredValuesString, Is.EqualTo(""));
        }

        [Test]
        public void DefaultConstructor_InitializesWithUnknownLocation()
        {
            // Act
            var status = new MeasurementStatus();

            // Assert
            Assert.That(status.Location, Is.EqualTo("Unknown"));
            Assert.That(status.IsPassed, Is.Null);
            Assert.That(status.MeasuredValuesString, Is.EqualTo(""));
        }

        [Test]
        public void Location_CanBeSetAndGet()
        {
            // Arrange
            var status = new MeasurementStatus();
            string newLocation = "TopLeft";

            // Act
            status.Location = newLocation;

            // Assert
            Assert.That(status.Location, Is.EqualTo(newLocation));
        }

        [Test]
        public void IsPassed_CanBeSetToTrue()
        {
            // Arrange
            var status = new MeasurementStatus();

            // Act
            status.IsPassed = true;

            // Assert
            Assert.That(status.IsPassed, Is.True);
        }

        [Test]
        public void IsPassed_CanBeSetToFalse()
        {
            // Arrange
            var status = new MeasurementStatus();

            // Act
            status.IsPassed = false;

            // Assert
            Assert.That(status.IsPassed, Is.False);
        }

        [Test]
        public void IsPassed_CanBeSetToNull()
        {
            // Arrange
            var status = new MeasurementStatus();
            status.IsPassed = true; // Set to true first

            // Act
            status.IsPassed = null; // Then set to null

            // Assert
            Assert.That(status.IsPassed, Is.Null);
        }

        [Test]
        public void StatusColor_WhenPassedIsTrue_ReturnsDarkGreen()
        {
            // Arrange
            var status = new MeasurementStatus();

            // Act
            status.IsPassed = true;

            // Assert
            Assert.That(status.StatusColor, Is.EqualTo(Brushes.DarkGreen));
        }

        [Test]
        public void StatusColor_WhenPassedIsFalse_ReturnsDarkRed()
        {
            // Arrange
            var status = new MeasurementStatus();

            // Act
            status.IsPassed = false;

            // Assert
            Assert.That(status.StatusColor, Is.EqualTo(Brushes.DarkRed));
        }

        [Test]
        public void StatusColor_WhenPassedIsNull_ReturnsDimGray()
        {
            // Arrange
            var status = new MeasurementStatus();

            // Act
            status.IsPassed = null;

            // Assert
            Assert.That(status.StatusColor, Is.EqualTo(Brushes.DimGray));
        }

        [Test]
        public void MeasuredValuesString_CanBeSetAndGet()
        {
            // Arrange
            var status = new MeasurementStatus();
            string measuredValues = "x: 0.312, y: 0.329, Lv: 250.5, T: 6500";

            // Act
            status.MeasuredValuesString = measuredValues;

            // Assert
            Assert.That(status.MeasuredValuesString, Is.EqualTo(measuredValues));
        }

        [Test]
        public void IsPassed_WhenChanged_RaisesPropertyChangedForStatusColor()
        {
            // Arrange
            var status = new MeasurementStatus();
            var propertyChangedEvents = new List<string>();

            status.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName != null)
                    propertyChangedEvents.Add(args.PropertyName);
            };

            // Act
            status.IsPassed = true;

            // Assert
            Assert.That(propertyChangedEvents, Does.Contain("IsPassed"));
            Assert.That(propertyChangedEvents, Does.Contain("StatusColor"));
        }

        [Test]
        public void MeasuredValuesString_WhenChanged_RaisesPropertyChanged()
        {
            // Arrange
            var status = new MeasurementStatus();
            bool propertyChangedRaised = false;

            status.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == "MeasuredValuesString")
                    propertyChangedRaised = true;
            };

            // Act
            status.MeasuredValuesString = "New values";

            // Assert
            Assert.That(propertyChangedRaised, Is.True);
        }

        [Test]
        public void StatusColor_ChangesWhenIsPassedChanges()
        {
            // Arrange
            var status = new MeasurementStatus();

            // Act & Assert - Initially null -> DimGray
            Assert.That(status.StatusColor, Is.EqualTo(Brushes.DimGray));

            // Set to true -> DarkGreen
            status.IsPassed = true;
            Assert.That(status.StatusColor, Is.EqualTo(Brushes.DarkGreen));

            // Set to false -> DarkRed
            status.IsPassed = false;
            Assert.That(status.StatusColor, Is.EqualTo(Brushes.DarkRed));

            // Set back to null -> DimGray
            status.IsPassed = null;
            Assert.That(status.StatusColor, Is.EqualTo(Brushes.DimGray));
        }

        [Test]
        public void MeasuredValuesString_DefaultValue_IsEmptyString()
        {
            // Arrange & Act
            var status = new MeasurementStatus();

            // Assert
            Assert.That(status.MeasuredValuesString, Is.EqualTo(""));
            Assert.That(status.MeasuredValuesString, Is.Not.Null);
        }
    }
}
