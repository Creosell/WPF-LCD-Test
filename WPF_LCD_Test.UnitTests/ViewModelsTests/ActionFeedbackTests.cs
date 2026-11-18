using Moq;
using WPF_LCD_Test.Models;
using WPF_LCD_Test.ViewModels;

namespace WPF_LCD_Test.UnitTests.ViewModelsTests
    {
    [TestFixture]
    public class ActionFeedbackSignalTests
        {
        [Test]
        public void Signals_SetTheOutcomeAndCountEverySignalEvenTheSameOneTwice()
            {
            var signal = new ActionFeedbackSignal();
            var sequences = new List<int>();
            signal.PropertyChanged += (_, e) =>
                {
                if (e.PropertyName == nameof(ActionFeedbackSignal.Sequence)) sequences.Add(signal.Sequence);
                };

            signal.Succeed();
            signal.Succeed();
            signal.Fail();

            Assert.That(signal.Feedback, Is.EqualTo(ActionFeedback.Failure));
            Assert.That(sequences, Is.EqualTo(new[] { 1, 2, 3 }));
            }

        [Test]
        public void NewSignal_HasNothingToShow()
            {
            var signal = new ActionFeedbackSignal();

            Assert.That((signal.Feedback, signal.Sequence), Is.EqualTo((ActionFeedback.None, 0)));
            }
        }
    }
