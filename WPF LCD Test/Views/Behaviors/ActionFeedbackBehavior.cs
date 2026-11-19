using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using WPF_LCD_Test.Models;

namespace WPF_LCD_Test.Views.Behaviors
{
    /// <summary>
    /// How an element shows the outcome of an action.
    /// </summary>
    public enum ActionFeedbackMode
    {
        /// <summary>The background of a button breathes, a glow pulses around it and its label names the outcome.</summary>
        Fill,

        /// <summary>The border brush (the underline) of a control breathes.</summary>
        Underline,

        /// <summary>A separate border element laid over an underline breathes; it is transparent at rest.</summary>
        Overlay,

        /// <summary>A colored glow around the element pulses; works on elements whose own background carries a status color.</summary>
        Glow
    }

    /// <summary>
    /// Attached behavior that plays the outcome feedback of an <see cref="ActionFeedbackSignal"/> on any element, wherever the page
    /// that hosts it is: <c>behaviors:ActionFeedbackBehavior.Signal="{Binding SaveFeedback}"</c>. The views need no code-behind for it.
    /// </summary>
    public static class ActionFeedbackBehavior
    {
        private static readonly ConditionalWeakTable<FrameworkElement, PropertyChangedEventHandler> Handlers = new();

        /// <summary>The signal the element listens to.</summary>
        public static readonly DependencyProperty SignalProperty = DependencyProperty.RegisterAttached(
            "Signal", typeof(ActionFeedbackSignal), typeof(ActionFeedbackBehavior), new PropertyMetadata(null, OnSignalChanged));

        /// <summary>Resource key of the label a <see cref="ActionFeedbackMode.Fill"/> element shows on success.</summary>
        public static readonly DependencyProperty SuccessTextProperty = DependencyProperty.RegisterAttached(
            "SuccessText", typeof(string), typeof(ActionFeedbackBehavior), new PropertyMetadata(null));

        /// <summary>How the element shows the outcome.</summary>
        public static readonly DependencyProperty ModeProperty = DependencyProperty.RegisterAttached(
            "Mode", typeof(ActionFeedbackMode), typeof(ActionFeedbackBehavior), new PropertyMetadata(ActionFeedbackMode.Fill));

        public static ActionFeedbackSignal? GetSignal(DependencyObject element) => (ActionFeedbackSignal?)element.GetValue(SignalProperty);

        public static void SetSignal(DependencyObject element, ActionFeedbackSignal? value) => element.SetValue(SignalProperty, value);

        public static string? GetSuccessText(DependencyObject element) => (string?)element.GetValue(SuccessTextProperty);

        public static void SetSuccessText(DependencyObject element, string? value) => element.SetValue(SuccessTextProperty, value);

        public static ActionFeedbackMode GetMode(DependencyObject element) => (ActionFeedbackMode)element.GetValue(ModeProperty);

        public static void SetMode(DependencyObject element, ActionFeedbackMode value) => element.SetValue(ModeProperty, value);

        private static void OnSignalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element) return;

            if (Handlers.TryGetValue(element, out var previous))
            {
                if (e.OldValue is ActionFeedbackSignal oldSignal) oldSignal.PropertyChanged -= previous;
                Handlers.Remove(element);
            }

            if (e.NewValue is not ActionFeedbackSignal signal) return;

            PropertyChangedEventHandler handler = (_, args) =>
            {
                if (args.PropertyName == nameof(ActionFeedbackSignal.Sequence)) Play(element, signal.Feedback);
            };
            Handlers.Add(element, handler);
            signal.PropertyChanged += handler;
        }

        private static void Play(FrameworkElement element, ActionFeedback feedback)
        {
            switch (GetMode(element))
            {
                case ActionFeedbackMode.Overlay when element is Border line:
                    UnderlineBreathing.PlayOverlay(line, feedback);
                    break;
                case ActionFeedbackMode.Underline when element is Control field:
                    UnderlineBreathing.Play(field, feedback);
                    break;
                case ActionFeedbackMode.Glow:
                    GlowBreathing.Play(element, feedback);
                    break;
                case ActionFeedbackMode.Fill when element is Control control:
                    ActionFeedbackBreathing.Play(control, feedback, GetSuccessText(element));
                    GlowBreathing.Play(control, feedback);
                    break;
            }
        }
    }

    /// <summary>
    /// Presentation-only quick feedback of an input field: its underline takes green or red and goes back;
    /// the exhale is slower than the inhale.
    /// </summary>
    internal static class UnderlineBreathing
    {
        private static readonly TimeSpan InhaleEnd = TimeSpan.FromMilliseconds(220);
        private static readonly TimeSpan HoldEnd = TimeSpan.FromMilliseconds(600);
        private static readonly TimeSpan ExhaleEnd = TimeSpan.FromMilliseconds(1300);

        /// <summary>
        /// Starts the breathing on a separate line laid over the underline of a field; the line is transparent at rest.
        /// </summary>
        /// <param name="line">The overlay line.</param>
        /// <param name="feedback">The outcome that selects the color.</param>
        public static void PlayOverlay(Border line, ActionFeedback feedback)
        {
            if (feedback == ActionFeedback.None) return;

            var target = FeedbackPalette.Line(line, feedback);
            var rest = Color.FromArgb(0, target.R, target.G, target.B);
            var brush = new SolidColorBrush(rest);
            line.Background = brush;

            var breathing = new ColorAnimationUsingKeyFrames { Duration = ExhaleEnd };
            breathing.KeyFrames.Add(new EasingColorKeyFrame(target, InhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
            breathing.KeyFrames.Add(new LinearColorKeyFrame(target, HoldEnd));
            breathing.KeyFrames.Add(new EasingColorKeyFrame(rest, ExhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            breathing.Completed += (_, _) =>
            {
                if (ReferenceEquals(line.Background, brush)) line.ClearValue(Border.BackgroundProperty);
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, breathing);
        }

        /// <summary>
        /// Starts the breathing on the underline (the border brush) of the control; a running one is restarted.
        /// </summary>
        /// <param name="field">The field whose underline breathes.</param>
        /// <param name="feedback">The outcome that selects the color.</param>
        public static void Play(Control field, ActionFeedback feedback)
        {
            if (feedback == ActionFeedback.None) return;

            field.ClearValue(Control.BorderBrushProperty);
            var rest = (field.BorderBrush as SolidColorBrush)?.Color ?? Colors.Gray;
            var target = FeedbackPalette.Line(field, feedback);
            var brush = new SolidColorBrush(rest);
            field.BorderBrush = brush;

            var breathing = new ColorAnimationUsingKeyFrames { Duration = ExhaleEnd };
            breathing.KeyFrames.Add(new EasingColorKeyFrame(target, InhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
            breathing.KeyFrames.Add(new LinearColorKeyFrame(target, HoldEnd));
            breathing.KeyFrames.Add(new EasingColorKeyFrame(rest, ExhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            breathing.Completed += (_, _) =>
            {
                if (ReferenceEquals(field.BorderBrush, brush)) field.ClearValue(Control.BorderBrushProperty);
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, breathing);
        }
    }

    /// <summary>
    /// Presentation-only feedback of a control button: the background breathes once between its own color and green
    /// (the action succeeded) or red (it failed or was canceled) and the label names the outcome while the color is held.
    /// The exhale back to the style is slower than the inhale.
    /// </summary>
    internal static class ActionFeedbackBreathing
    {
        private static readonly TimeSpan InhaleEnd = TimeSpan.FromMilliseconds(450);
        private static readonly TimeSpan HoldEnd = TimeSpan.FromMilliseconds(1700);
        private static readonly TimeSpan ExhaleEnd = TimeSpan.FromMilliseconds(3400);
        private static readonly TimeSpan LabelFadeOutStart = TimeSpan.FromMilliseconds(3100);
        private static readonly TimeSpan LabelFadeInEnd = TimeSpan.FromMilliseconds(3900);

        /// <summary>
        /// Starts the breathing on the button; a running one is restarted from the button's own color.
        /// </summary>
        /// <param name="button">The button that finished its action.</param>
        /// <param name="feedback">The outcome that selects the color and the label.</param>
        /// <param name="successTextKey">Resource key of the label shown on success.</param>
        public static void Play(Control button, ActionFeedback feedback, string? successTextKey)
        {
            if (feedback == ActionFeedback.None) return;

            button.BeginAnimation(ContentControl.ContentProperty, null);
            button.ClearValue(Control.ForegroundProperty);
            button.ClearValue(Control.BackgroundProperty);
            var target = FeedbackPalette.Line(button, feedback);
            var baseColor = (button.Background as SolidColorBrush)?.Color ?? Colors.Transparent;
            if (baseColor.A == 0) baseColor = Color.FromArgb(0, target.R, target.G, target.B);
            var brush = new SolidColorBrush(baseColor);
            button.Background = brush;

            var breathing = new ColorAnimationUsingKeyFrames { Duration = ExhaleEnd };
            breathing.KeyFrames.Add(new EasingColorKeyFrame(target, InhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
            breathing.KeyFrames.Add(new LinearColorKeyFrame(target, HoldEnd));
            breathing.KeyFrames.Add(new EasingColorKeyFrame(baseColor, ExhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            breathing.Completed += (_, _) =>
            {
                if (ReferenceEquals(button.Background, brush)) button.ClearValue(Control.BackgroundProperty);
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, breathing);

            string? textKey = successTextKey == null ? null : feedback switch
            {
                ActionFeedback.Success => successTextKey,
                ActionFeedback.Canceled => "FeedbackCanceled",
                _ => "FeedbackFailed"
            };
            if (textKey != null && button is ContentControl && button.TryFindResource(textKey) is string text)
            {
                var label = new ObjectAnimationUsingKeyFrames { Duration = ExhaleEnd, FillBehavior = FillBehavior.Stop };
                label.KeyFrames.Add(new DiscreteObjectKeyFrame(text.Trim(), TimeSpan.Zero));
                button.BeginAnimation(ContentControl.ContentProperty, label);
                FadeLabelOver(button);
            }
        }

        /// <summary>
        /// Fades the label out at the end of the exhale and fades the button's own text in after the swap.
        /// </summary>
        private static void FadeLabelOver(Control button)
        {
            if (button.Foreground is not SolidColorBrush original) return;

            var brush = new SolidColorBrush(original.Color);
            button.Foreground = brush;
            var fade = new ColorAnimationUsingKeyFrames { Duration = LabelFadeInEnd };
            fade.KeyFrames.Add(new LinearColorKeyFrame(original.Color, LabelFadeOutStart));
            fade.KeyFrames.Add(new EasingColorKeyFrame(Color.FromArgb(0, original.Color.R, original.Color.G, original.Color.B), ExhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseIn } });
            fade.KeyFrames.Add(new EasingColorKeyFrame(original.Color, LabelFadeInEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
            fade.Completed += (_, _) =>
            {
                if (ReferenceEquals(button.Foreground, brush)) button.ClearValue(Control.ForegroundProperty);
            };
            brush.BeginAnimation(SolidColorBrush.ColorProperty, fade);
        }
    }

    /// <summary>
    /// Presentation-only feedback that does not touch the background of the element: a colored glow around it pulses
    /// (green: success, red: failure) and fades; the exhale is slower than the inhale.
    /// </summary>
    internal static class GlowBreathing
    {
        private static readonly TimeSpan InhaleEnd = TimeSpan.FromMilliseconds(200);
        private static readonly TimeSpan HoldEnd = TimeSpan.FromMilliseconds(450);
        private static readonly TimeSpan ExhaleEnd = TimeSpan.FromMilliseconds(1300);
        private const double PeakRadius = 16;
        private const double PeakOpacity = 0.85;

        /// <summary>
        /// Starts the glow on the element; a running one is restarted.
        /// </summary>
        /// <param name="element">The element that glows.</param>
        /// <param name="feedback">The outcome that selects the color.</param>
        public static void Play(FrameworkElement element, ActionFeedback feedback)
        {
            if (feedback == ActionFeedback.None) return;

            var glow = new DropShadowEffect
            {
                Color = FeedbackPalette.Glow(element, feedback),
                ShadowDepth = 0,
                BlurRadius = 0,
                Opacity = 0
            };
            element.Effect = glow;

            glow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, Pulse(PeakRadius));
            var opacity = Pulse(PeakOpacity);
            opacity.Completed += (_, _) =>
            {
                if (ReferenceEquals(element.Effect, glow)) element.ClearValue(UIElement.EffectProperty);
            };
            glow.BeginAnimation(DropShadowEffect.OpacityProperty, opacity);
        }

        private static DoubleAnimationUsingKeyFrames Pulse(double peak)
        {
            var pulse = new DoubleAnimationUsingKeyFrames { Duration = ExhaleEnd };
            pulse.KeyFrames.Add(new EasingDoubleKeyFrame(peak, InhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseOut } });
            pulse.KeyFrames.Add(new LinearDoubleKeyFrame(peak, HoldEnd));
            pulse.KeyFrames.Add(new EasingDoubleKeyFrame(0, ExhaleEnd) { EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } });
            return pulse;
        }
    }
}
