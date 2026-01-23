namespace WPF_LCD_Test.Models
    {
    /// <summary>
    /// Defines measurement locations and color test points on the device under test.
    /// </summary>
    public enum MeasurementLocation
        {
        /// <summary>Top left position.</summary>
        TopLeft,
        /// <summary>Top center position.</summary>
        TopCenter,
        /// <summary>Top right position.</summary>
        TopRight,
        /// <summary>Middle left position.</summary>
        MiddleLeft,
        /// <summary>Center position.</summary>
        Center,
        /// <summary>Middle right position.</summary>
        MiddleRight,
        /// <summary>Bottom left position.</summary>
        BottomLeft,
        /// <summary>Bottom center position.</summary>
        BottomCenter,
        /// <summary>Bottom right position.</summary>
        BottomRight,
        /// <summary>Red color test point.</summary>
        RedColor,
        /// <summary>Green color test point.</summary>
        GreenColor,
        /// <summary>Blue color test point.</summary>
        BlueColor,
        /// <summary>White color test point.</summary>
        WhiteColor,
        /// <summary>Black color test point.</summary>
        BlackColor
        }
    }