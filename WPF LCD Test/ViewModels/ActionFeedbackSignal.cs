using MvvmHelpers;

namespace WPF_LCD_Test.ViewModels
    {
    /// <summary>
    /// Outcome of an action of a control button that the view signals to the operator.
    /// </summary>
    public enum ActionFeedback
        {
        /// <summary>Nothing to signal.</summary>
        None,

        /// <summary>The action finished successfully.</summary>
        Success,

        /// <summary>The action failed.</summary>
        Failure
        }

    /// <summary>
    /// Tells the view that the action of a button finished, so it can play the feedback (the button breathes green or red).
    /// The sequence number changes on every signal, so two successes in a row are both noticed.
    /// </summary>
    public sealed class ActionFeedbackSignal : BaseViewModel
        {
        private ActionFeedback _feedback;
        private int _sequence;

        /// <summary>
        /// Gets the outcome of the last signaled action.
        /// </summary>
        public ActionFeedback Feedback
            {
            get => _feedback;
            private set => SetProperty(ref _feedback, value);
            }

        /// <summary>
        /// Gets the number of signals so far; the view plays the feedback when it changes.
        /// </summary>
        public int Sequence
            {
            get => _sequence;
            private set => SetProperty(ref _sequence, value);
            }

        /// <summary>
        /// Signals that the action finished successfully.
        /// </summary>
        public void Succeed() => Raise(ActionFeedback.Success);

        /// <summary>
        /// Signals that the action failed.
        /// </summary>
        public void Fail() => Raise(ActionFeedback.Failure);

        private void Raise(ActionFeedback feedback)
            {
            Feedback = feedback;
            Sequence++;
            }
        }
    }
