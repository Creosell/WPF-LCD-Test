#if DEBUG
using System.ComponentModel;
using System.Reflection;

namespace WPF_LCD_Test.DebugTools
    {
    /// <summary>
    /// Traces every property change a view model announces, with the new value, so the state of a page can be followed without breakpoints.
    /// </summary>
    public static class ViewModelTracer
        {
        /// <summary>
        /// Starts tracing the property changes of the view model.
        /// </summary>
        /// <typeparam name="T">The type of the view model.</typeparam>
        /// <param name="viewModel">The view model.</param>
        /// <returns>The same view model.</returns>
        public static T Watch<T>(T viewModel) where T : INotifyPropertyChanged
            {
            var type = viewModel.GetType();
            viewModel.PropertyChanged += (_, e) =>
                DebugTrace.Write("state", $"{type.Name}.{(string.IsNullOrEmpty(e.PropertyName) ? "*" : e.PropertyName)} = {ReadValue(type, viewModel, e.PropertyName)}");
            return viewModel;
            }

        private static string ReadValue(Type type, object viewModel, string? propertyName)
            {
            if (string.IsNullOrEmpty(propertyName)) return "(all properties)";

            try
                {
                var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                return property == null || property.GetIndexParameters().Length > 0 ? "(not readable)" : DebugTrace.Format(property.GetValue(viewModel));
                }
            catch (Exception ex) when (ex is TargetInvocationException or AmbiguousMatchException)
                {
                return "(not readable)";
                }
            }
        }
    }
#endif
