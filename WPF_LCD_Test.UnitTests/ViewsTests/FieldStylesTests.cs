using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace WPF_LCD_Test.UnitTests.ViewsTests
    {
    /// <summary>
    /// Draws the shared control styles of <c>Resources/Styles/FieldStyles.xaml</c> in a real (off-screen) window and checks what the user sees:
    /// the chosen value, the items of the list, the underline that grows and stays filled while the list is open.
    /// </summary>
    [TestFixture]
    public class FieldStylesTests
        {
        private sealed record Option(string Name)
            {
            public override string ToString() => nameof(Option);
            }

        private Thread _uiThread = null!;
        private Dispatcher _dispatcher = null!;
        private ResourceDictionary _resources = null!;

        [OneTimeSetUp]
        public void StartUiThread()
            {
            var started = new ManualResetEventSlim();
            _uiThread = new Thread(() =>
                {
                _dispatcher = Dispatcher.CurrentDispatcher;
                if (Application.Current == null) _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

                _resources = new ResourceDictionary();
                _resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Screen Checker;component/Resources/Themes/Theme.Dark.xaml") });
                _resources.MergedDictionaries.Add(new BundledTheme { BaseTheme = BaseTheme.Dark, PrimaryColor = MaterialDesignColors.PrimaryColor.DeepPurple, SecondaryColor = MaterialDesignColors.SecondaryColor.Lime });
                _resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml") });
                _resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/Screen Checker;component/Resources/Styles/FieldStyles.xaml") });

                started.Set();
                Dispatcher.Run();
                }) { IsBackground = true };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
            started.Wait();
            }

        [OneTimeTearDown]
        public void StopUiThread() => _dispatcher.InvokeShutdown();

        /// <summary>Runs the body on the UI thread and rethrows what it threw.</summary>
        private void Ui(Action body)
            {
            Exception? error = null;
            _dispatcher.Invoke(() =>
                {
                try { body(); }
                catch (Exception e) { error = e; }
                });
            if (error != null) ExceptionDispatchInfo.Capture(error).Throw();
            }

        /// <summary>Lets the UI thread work (layout, animations) for the time.</summary>
        private static void Pump(int milliseconds)
            {
            var frame = new DispatcherFrame();
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(milliseconds) };
            timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
            timer.Start();
            Dispatcher.PushFrame(frame);
            }

        private Window Show(FrameworkElement content)
            {
            var window = new Window
                {
                Resources = _resources,
                Content = new StackPanel { Margin = new Thickness(20), Children = { content } },
                Left = -10000,
                Top = -10000,
                Width = 400,
                Height = 300,
                WindowStyle = WindowStyle.None,
                ShowActivated = false,
                ShowInTaskbar = false
                };
            window.Show();
            Pump(200);
            return window;
            }

        private ComboBox NewComboBox() => new()
            {
            Style = (Style)_resources["UnderlinedComboBoxStyle"],
            Width = 200,
            ItemsSource = new[] { new Option("Dark"), new Option("Light"), new Option("System") },
            DisplayMemberPath = nameof(Option.Name),
            SelectedIndex = 0
            };

        private static Border Part(Control owner, string name) => (Border)owner.Template.FindName(name, owner);

        private static double Scale(Control owner, string accent) => ((ScaleTransform)Part(owner, accent).RenderTransform).ScaleX;

        private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
            {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) yield return match;
                foreach (var nested in Descendants<T>(child)) yield return nested;
                }
            }

        [Test]
        public void ComboBox_ShowsTheDisplayNameOfTheChosenItem()
            {
            Ui(() =>
                {
                var combo = NewComboBox();
                var window = Show(combo);

                var texts = Descendants<TextBlock>(combo).Select(t => t.Text).ToList();
                window.Close();

                Assert.That(texts, Does.Contain("Dark"), "the chosen value is its display name, not the type name");
                Assert.That(texts, Does.Not.Contain(nameof(Option)));
                });
            }

        [Test]
        public void ComboBox_ListsTheDisplayNamesOfTheItems()
            {
            Ui(() =>
                {
                var combo = NewComboBox();
                var window = Show(combo);

                combo.IsDropDownOpen = true;
                Pump(300);
                var names = Enumerable.Range(0, 3)
                    .Select(i => Descendants<TextBlock>((DependencyObject)combo.ItemContainerGenerator.ContainerFromIndex(i)).Select(t => t.Text).FirstOrDefault())
                    .ToList();
                window.Close();

                Assert.That(names, Is.EqualTo(new[] { "Dark", "Light", "System" }));
                });
            }

        [Test]
        public void ComboBox_OpensAndClosesTheList()
            {
            Ui(() =>
                {
                var combo = NewComboBox();
                var window = Show(combo);

                combo.IsDropDownOpen = true;
                Pump(100);
                bool opened = combo.IsDropDownOpen;
                combo.IsDropDownOpen = false;
                Pump(100);
                bool closed = !combo.IsDropDownOpen;
                window.Close();

                Assert.That((opened, closed), Is.EqualTo((true, true)));
                });
            }

        [Test]
        public void ComboBox_TheUnderlineStaysFilledWhileTheListIsOpen()
            {
            Ui(() =>
                {
                var combo = NewComboBox();
                var window = Show(combo);
                double atRest = Scale(combo, "FocusAccent");

                combo.IsDropDownOpen = true;
                Pump(800);
                double whileOpen = Scale(combo, "FocusAccent");

                combo.IsDropDownOpen = false;
                Pump(800);
                double afterClose = Scale(combo, "FocusAccent");
                window.Close();

                Assert.That(atRest, Is.EqualTo(0), "at rest only the gray underline is drawn");
                Assert.That(whileOpen, Is.EqualTo(1).Within(0.001), "the line of the theme color fills the underline while the list is open, wherever the mouse is");
                Assert.That(afterClose, Is.EqualTo(0).Within(0.001));
                });
            }

        private sealed class LiveOption(string name) : System.ComponentModel.INotifyPropertyChanged
            {
            private string _name = name;

            public string Name
                {
                get => _name;
                set
                    {
                    _name = value;
                    PropertyChanged?.Invoke(this, new(nameof(Name)));
                    }
                }

            public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

            public override string ToString() => nameof(LiveOption);
            }

        [Test]
        public void ComboBox_UpdatesTheShownNameWhenTheNameOfTheChosenItemChanges()
            {
            Ui(() =>
                {
                var chosen = new LiveOption("Dark");
                var combo = NewComboBox();
                combo.ItemsSource = new[] { chosen, new LiveOption("Light") };
                combo.SelectedIndex = 0;
                var window = Show(combo);

                chosen.Name = "深色";
                Pump(100);
                var texts = Descendants<TextBlock>(combo).Select(t => t.Text).ToList();
                window.Close();

                Assert.That(texts, Does.Contain("深色"), "the names of the theme list follow the language of the interface");
                });
            }

        [Test]
        public void ComboBox_ShowsTheChosenItemAfterTheSelectionChanges()
            {
            Ui(() =>
                {
                var combo = NewComboBox();
                var window = Show(combo);

                combo.SelectedIndex = 1;
                Pump(100);
                var texts = Descendants<TextBlock>(combo).Select(t => t.Text).ToList();
                window.Close();

                Assert.That(texts, Does.Contain("Light"));
                });
            }

        [Test]
        public void DropdownButton_TheUnderlineStaysFilledWhileItsMenuIsOpen()
            {
            Ui(() =>
                {
                var menu = new ContextMenu { Items = { new MenuItem { Header = "Config" } }, Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom, StaysOpen = true };
                var button = new Button { Style = (Style)_resources["UnderlinedDropdownButtonStyle"], Content = "Choose", Width = 200, ContextMenu = menu };
                var window = Show(button);
                menu.PlacementTarget = button;
                var primary = ((SolidColorBrush)_resources["MaterialDesign.Brush.Primary"]).Color;
                var line = ((SolidColorBrush)_resources["AppFieldLine"]).Color;
                Color Underline() => ((SolidColorBrush)Part(button, "Underline").BorderBrush).Color;
                var atRest = Underline();

                menu.IsOpen = true;
                Pump(300);
                var whileOpen = Underline();

                menu.IsOpen = false;
                Pump(300);
                var afterClose = Underline();
                window.Close();

                Assert.That((atRest, whileOpen, afterClose), Is.EqualTo((line, primary, line)));
                });
            }

        [TestCase(3)]
        [TestCase(40)]
        [TestCase(100)]
        [TestCase(160)]
        [TestCase(197)]
        public void ComboBox_EveryPointOfTheFieldOpensTheList(double x)
            {
            Ui(() =>
                {
                var combo = NewComboBox();
                var window = Show(combo);

                var hit = VisualTreeHelper.HitTest(combo, new Point(x, combo.ActualHeight / 2))?.VisualHit;
                bool onToggle = false;
                for (var current = hit; current != null && !ReferenceEquals(current, combo); current = VisualTreeHelper.GetParent(current))
                    {
                    onToggle |= current is System.Windows.Controls.Primitives.ToggleButton;
                    }
                var toggle = Descendants<System.Windows.Controls.Primitives.ToggleButton>(combo).First();
                var size = (toggle.ActualWidth, toggle.ActualHeight, combo.ActualWidth, combo.ActualHeight);
                window.Close();

                Assert.That(onToggle, $"a click at x={x} reaches the toggle that opens the list (toggle, combo sizes: {size}); it hit {hit?.GetType().Name}");
                });
            }

        private ComboBox NewFitComboBox(params string[] names)
            {
            var combo = NewComboBox();
            combo.Width = double.NaN;
            combo.HorizontalAlignment = HorizontalAlignment.Left;
            combo.ItemsSource = names.Select(n => new Option(n)).ToArray();
            combo.SelectedIndex = 0;
            return combo;
            }

        private static double TextWidth(Control owner, string text) =>
            new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(owner.FontFamily, owner.FontStyle, owner.FontWeight, owner.FontStretch), owner.FontSize, Brushes.Black,
                VisualTreeHelper.GetDpi(owner).PixelsPerDip).Width;

        [Test]
        public void ComboBox_IsAsWideAsItsWidestItemWhateverIsChosen()
            {
            Ui(() =>
                {
                var combo = NewFitComboBox("UNIV", "Internal synchronization", "NTSC");
                var window = Show(combo);

                double shortChosen = combo.ActualWidth;
                combo.SelectedIndex = 1;
                Pump(100);
                double longChosen = combo.ActualWidth;
                double widest = TextWidth(combo, "Internal synchronization");
                window.Close();

                Assert.Multiple(() =>
                    {
                    Assert.That(shortChosen, Is.EqualTo(longChosen).Within(0.5), "the width does not jump when another item is chosen");
                    Assert.That(shortChosen, Is.GreaterThanOrEqualTo(widest), "the widest item fits");
                    Assert.That(shortChosen, Is.LessThan(widest + 40), "and nothing much more than that (the arrow and a gap)");
                    });
                });
            }

        [Test]
        public void ComboBox_FollowsTheWidestItemWhenItsNameChanges()
            {
            Ui(() =>
                {
                var chosen = new LiveOption("Dark");
                var combo = NewFitComboBox("Dark");
                combo.ItemsSource = new[] { chosen, new LiveOption("Light") };
                combo.SelectedIndex = 0;
                var window = Show(combo);

                double before = combo.ActualWidth;
                chosen.Name = "System (Windows), follows the theme of the system";
                Pump(200);
                double after = combo.ActualWidth;
                window.Close();

                Assert.That(after, Is.GreaterThan(before + 100), "the list grows with the longest name (the names follow the language of the interface)");
                });
            }

        [Test]
        public void ComboBox_FitsInsideTheWindowWhenTheListIsLong()
            {
            Ui(() =>
                {
                var combo = NewFitComboBox("short", "a rather long name of the mode", "x");
                var window = Show(combo);

                var host = (FrameworkElement)combo.Parent;
                double width = combo.ActualWidth;
                double room = host.ActualWidth;
                window.Close();

                Assert.That(width, Is.LessThanOrEqualTo(room));
                });
            }

        [Test]
        public void ControlsOfOneRow_ShareTheTypeOfTheText()
            {
            Ui(() =>
                {
                var page = new UserControl { FontSize = (double)_resources["AppFontSize"] };
                page.SetResourceReference(Control.ForegroundProperty, "AppTextPrimary");
                var label = new Label { Content = "Label" };
                var box = new TextBox { Style = (Style)_resources["UnderlinedTextBoxStyle"], Text = "Field" };
                var combo = NewFitComboBox("Option");
                var dropdown = new Button { Style = (Style)_resources["UnderlinedDropdownButtonStyle"], Content = "Dropdown" };
                page.Content = new StackPanel { Children = { label, box, combo, dropdown } };
                var window = Show(page);

                Control[] controls = [label, box, combo, dropdown];
                var looks = controls
                    .Select(c => (c.GetType().Name, Look: $"{c.FontFamily.Source} {c.FontSize} {c.FontWeight} {(c.Foreground as SolidColorBrush)?.Color}"))
                    .ToList();
                window.Close();

                Assert.That(looks.Select(l => l.Look).Distinct().Count(), Is.EqualTo(1), string.Join("; ", looks.Select(l => $"{l.Name}: {l.Look}")));
                });
            }

        [Test]
        public void ComboBox_TheGrowingLineHasTheThicknessOfTheUnderline()
            {
            Ui(() =>
                {
                var combo = NewComboBox();
                var window = Show(combo);

                var underline = Part(combo, "Line");
                var focus = Part(combo, "FocusAccent");
                window.Close();

                Assert.That((underline.BorderThickness.Bottom, focus.Height), Is.EqualTo((1.0, 1.0)));
                });
            }

        [Test]
        public void TextBox_HasTheGrowingLinesOfTheSameThickness()
            {
            Ui(() =>
                {
                var box = new TextBox { Style = (Style)_resources["UnderlinedTextBoxStyle"], Width = 200 };
                var window = Show(box);

                var underline = Part(box, "Line");
                var hover = Part(box, "HoverAccent");
                var focus = Part(box, "FocusAccent");
                window.Close();

                Assert.That((underline.BorderThickness.Bottom, hover.Height, focus.Height), Is.EqualTo((1.0, 1.0, 1.0)));
                });
            }
        }
    }
