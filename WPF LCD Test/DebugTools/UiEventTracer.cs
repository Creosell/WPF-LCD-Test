#if DEBUG
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace WPF_LCD_Test.DebugTools
    {
    /// <summary>
    /// Traces what the user does to the interface: mouse presses, keys, clicks, toggles, selections, typing, lists and menus that open and
    /// close, windows. The events are caught by class handlers on the whole application, so a page needs no code for it.
    /// </summary>
    public static class UiEventTracer
        {
        private const int MaxLabelLength = 40;
        private static readonly ConditionalWeakTable<object, object> Hooked = new();
        private static bool _installed;
        private static RoutedEventArgs? _lastEvent;

        /// <summary>
        /// Starts tracing the user interface events of the application, the errors that nobody handles and the failed bindings.
        /// </summary>
        public static void Install()
            {
            if (_installed) return;
            _installed = true;

            Handle<UIElement>(Mouse.PreviewMouseDownEvent, new MouseButtonEventHandler((_, e) =>
                {
                if (!First(e)) return;
                DebugTrace.Write("ui", $"mouse {e.ChangedButton} down on {Describe(Target(e.OriginalSource))} (hit {e.OriginalSource.GetType().Name}){(e.ClickCount > 1 ? $" x{e.ClickCount}" : "")}");
                }));
            Handle<UIElement>(Keyboard.PreviewKeyDownEvent, new KeyEventHandler((_, e) =>
                {
                if (e.IsRepeat || !First(e)) return;
                DebugTrace.Write("ui", $"key {Modifiers()}{(e.Key == Key.System ? e.SystemKey : e.Key)} in {Describe(Keyboard.FocusedElement)}");
                }));
            Handle<ButtonBase>(ButtonBase.ClickEvent, new RoutedEventHandler((s, _) => DebugTrace.Write("ui", $"click {Describe(s)}")));
            Handle<MenuItem>(MenuItem.ClickEvent, new RoutedEventHandler((s, _) => DebugTrace.Write("ui", $"menu item {Describe(s)}")));
            Handle<ToggleButton>(ToggleButton.CheckedEvent, new RoutedEventHandler((s, _) => DebugTrace.Write("ui", $"checked {Describe(s)}")));
            Handle<ToggleButton>(ToggleButton.UncheckedEvent, new RoutedEventHandler((s, _) => DebugTrace.Write("ui", $"unchecked {Describe(s)}")));
            Handle<Selector>(Selector.SelectionChangedEvent, new SelectionChangedEventHandler((s, e) =>
                {
                if (!ReferenceEquals(s, e.OriginalSource)) return;
                DebugTrace.Write("ui", $"selection in {Describe(s)}: {Join(e.RemovedItems)} -> {Join(e.AddedItems)}");
                }));
            Handle<TextBoxBase>(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((s, _) =>
                DebugTrace.Write("ui", $"text of {Describe(s)} = {DebugTrace.Format((s as TextBox)?.Text)}")));
            Handle<UIElement>(Keyboard.GotKeyboardFocusEvent, new KeyboardFocusChangedEventHandler((s, e) =>
                {
                if (ReferenceEquals(s, e.NewFocus)) DebugTrace.Write("ui", $"focus {Describe(e.OldFocus)} -> {Describe(e.NewFocus)}");
                }));
            Handle<ContextMenu>(ContextMenu.OpenedEvent, new RoutedEventHandler((s, _) => DebugTrace.Write("ui", $"context menu opened on {Describe((s as ContextMenu)?.PlacementTarget)}")));
            Handle<ContextMenu>(ContextMenu.ClosedEvent, new RoutedEventHandler((_, _) => DebugTrace.Write("ui", "context menu closed")));
            Handle<FrameworkElement>(FrameworkElement.LoadedEvent, new RoutedEventHandler((s, _) => HookOnLoad(s)));

            Application.Current.DispatcherUnhandledException += (_, e) => DebugTrace.Write("error", $"unhandled UI exception: {e.Exception}");
            AppDomain.CurrentDomain.UnhandledException += (_, e) => DebugTrace.Write("error", $"unhandled exception (terminating: {e.IsTerminating}): {e.ExceptionObject}");
            TaskScheduler.UnobservedTaskException += (_, e) => DebugTrace.Write("error", $"unobserved task exception: {e.Exception}");

            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new BindingErrorListener());
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning;
            }

        /// <summary>
        /// A class handler runs for every element on the route of an event; true only for the first of them.
        /// </summary>
        private static bool First(RoutedEventArgs e)
            {
            if (ReferenceEquals(_lastEvent, e)) return false;

            _lastEvent = e;
            return true;
            }

        private static void Handle<T>(RoutedEvent routedEvent, Delegate handler) =>
            EventManager.RegisterClassHandler(typeof(T), routedEvent, handler, handledEventsToo: true);

        /// <summary>
        /// Hooks the events that are not routed (list opening, window life) once, when the element appears.
        /// </summary>
        private static void HookOnLoad(object sender)
            {
            if (sender is not (ComboBox or Window) || Hooked.TryGetValue(sender, out _)) return;
            Hooked.Add(sender, true);

            switch (sender)
                {
                case ComboBox combo:
                    combo.DropDownOpened += (s, _) => DebugTrace.Write("ui", $"list opened {Describe(s)}");
                    combo.DropDownClosed += (s, _) => DebugTrace.Write("ui", $"list closed {Describe(s)}");
                    break;
                case Window window:
                    DebugTrace.Write("ui", $"window loaded {Describe(window)}");
                    window.Activated += (s, _) => DebugTrace.Write("ui", $"window activated {Describe(s)}");
                    window.Deactivated += (s, _) => DebugTrace.Write("ui", $"window deactivated {Describe(s)}");
                    window.Closing += (s, e) => DebugTrace.Write("ui", $"window closing {Describe(s)}");
                    window.Closed += (s, _) => DebugTrace.Write("ui", $"window closed {Describe(s)}");
                    break;
                }
            }

        /// <summary>
        /// Finds the control the user means when the pointer hit a part of its template: the nearest button, field, list, tab or menu item.
        /// </summary>
        private static object? Target(object? hit)
            {
            for (var current = hit as DependencyObject; current != null; current = Parent(current))
                {
                if (current is ButtonBase or TextBoxBase or Selector or MenuItem or TabItem or ScrollBar or Slider) return current;
                }

            return hit;
            }

        private static DependencyObject? Parent(DependencyObject element) =>
            element is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(element) ?? LogicalTreeHelper.GetParent(element) : LogicalTreeHelper.GetParent(element);

        /// <summary>
        /// Names an element for a trace line: its type, name and the visible label.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>Text like <c>Button#SaveResultsButton "Save results"</c>.</returns>
        public static string Describe(object? element)
            {
            if (element == null) return "nothing";

            string name = element switch
                {
                FrameworkElement { Name.Length: > 0 } fe => $"#{fe.Name}",
                FrameworkContentElement { Name.Length: > 0 } fce => $"#{fce.Name}",
                _ => ""
                };
            string? label = element is DependencyObject d ? Label(d) : null;
            return $"{element.GetType().Name}{name}{(label == null ? "" : $" \"{label}\"")}";
            }

        private static string? Label(DependencyObject element)
            {
            string? label = element switch
                {
                TextBlock text => text.Text,
                HeaderedContentControl { Header: string header } => header,
                HeaderedItemsControl { Header: string header } => header,
                ContentControl { Content: string content } => content,
                ContentControl { Content: DependencyObject content } => FirstText(content),
                _ => null
                };
            label ??= System.Windows.Automation.AutomationProperties.GetName(element);

            if (string.IsNullOrWhiteSpace(label)) return null;
            label = label.Trim().Replace("\r", " ").Replace("\n", " ");
            return label.Length > MaxLabelLength ? label[..MaxLabelLength] + "…" : label;
            }

        private static string? FirstText(DependencyObject root)
            {
            if (root is TextBlock text) return text.Text;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                {
                var found = FirstText(VisualTreeHelper.GetChild(root, i));
                if (found != null) return found;
                }

            return null;
            }

        private static string Join(System.Collections.IList items) =>
            items.Count == 0 ? "nothing" : string.Join(", ", items.Cast<object?>().Select(item => item is DependencyObject ? Describe(item) : DebugTrace.Format(item)));

        private static string Modifiers()
            {
            var modifiers = Keyboard.Modifiers;
            return modifiers == ModifierKeys.None ? "" : modifiers.ToString().Replace(", ", "+") + "+";
            }
        }

    /// <summary>
    /// Forwards the warnings of the data binding engine (a path that does not exist, a converter that fails) to the trace.
    /// </summary>
    internal sealed class BindingErrorListener : TraceListener
        {
        private readonly System.Text.StringBuilder _line = new();

        public override void Write(string? message) => _line.Append(message);

        public override void WriteLine(string? message)
            {
            _line.Append(message);
            DebugTrace.Write("binding", _line.ToString().Trim());
            _line.Clear();
            }
        }
    }
#endif
