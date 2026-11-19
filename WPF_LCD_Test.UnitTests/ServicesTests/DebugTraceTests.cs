#if DEBUG
using System.Diagnostics;
using WPF_LCD_Test.Commands;
using WPF_LCD_Test.DebugTools;

namespace WPF_LCD_Test.UnitTests.ServicesTests
    {
    [TestFixture]
    [NonParallelizable]
    public class DebugTraceTests
        {
        public interface ICalculator
            {
            int Count { get; }

            int Add(int a, int b);

            void Fail();

            Task<string> LoadAsync(string name);
            }

        private sealed class Calculator : ICalculator
            {
            public int Count => 7;

            public int Add(int a, int b) => a + b;

            public void Fail() => throw new InvalidOperationException("broken");

            public Task<string> LoadAsync(string name) => Task.FromResult(name.ToUpperInvariant());
            }

        private sealed class Named
            {
            public override string ToString() => "named object";
            }

        private StringWriter _trace = null!;
        private TextWriterTraceListener _listener = null!;

        [SetUp]
        public void SetUp()
            {
            _trace = new StringWriter();
            _listener = new TextWriterTraceListener(_trace);
            Trace.Listeners.Add(_listener);
            }

        [TearDown]
        public void TearDown()
            {
            Trace.Listeners.Remove(_listener);
            _listener.Dispose();
            _trace.Dispose();
            }

        private string Lines() => _trace.ToString();

        [Test]
        public void Format_ShortensValuesToOneToken()
            {
            Assert.Multiple(() =>
                {
                Assert.That(DebugTrace.Format(null), Is.EqualTo("null"));
                Assert.That(DebugTrace.Format("a\nb"), Is.EqualTo("\"a\\nb\""));
                Assert.That(DebugTrace.Format(42), Is.EqualTo("42"));
                Assert.That(DebugTrace.Format(new[] { 1, 2, 3 }), Is.EqualTo("Int32[][3]"));
                Assert.That(DebugTrace.Format(new Named()), Is.EqualTo("named object"));
                Assert.That(DebugTrace.Format(new object()), Is.EqualTo("Object"));
                Assert.That(DebugTrace.Format(new string('x', 500)), Has.Length.LessThanOrEqualTo(210));
                });
            }

        [Test]
        public void DescribeDelegate_NamesTheMethodOfALambda()
            {
            Action lambda = () => { };

            Assert.That(DebugTrace.DescribeDelegate(lambda), Is.EqualTo("DebugTraceTests.DescribeDelegate_NamesTheMethodOfALambda"));
            }

        [Test]
        public void TracingProxy_ForwardsTheCallsAndTracesThem()
            {
            var calculator = TracingProxy.Wrap<ICalculator>(new Calculator());

            Assert.That(calculator.Add(2, 3), Is.EqualTo(5));
            Assert.That(calculator.Count, Is.EqualTo(7));
            Assert.Multiple(() =>
                {
                Assert.That(Lines(), Does.Contain("ICalculator.Add(2, 3)"));
                Assert.That(Lines(), Does.Contain("ICalculator.Add(2, 3) -> 5"));
                Assert.That(Lines(), Does.Not.Contain("get_Count"), "reading a property is not traced");
                });
            }

        [Test]
        public void TracingProxy_RethrowsTheOriginalException()
            {
            var calculator = TracingProxy.Wrap<ICalculator>(new Calculator());

            var thrown = Assert.Throws<InvalidOperationException>(calculator.Fail);

            Assert.That(thrown!.Message, Is.EqualTo("broken"));
            Assert.That(Lines(), Does.Contain("ICalculator.Fail() threw InvalidOperationException: broken"));
            }

        [Test]
        public async Task TracingProxy_TracesTheCompletionOfAnAsyncCall()
            {
            var calculator = TracingProxy.Wrap<ICalculator>(new Calculator());

            string result = await calculator.LoadAsync("abc");
            await Task.Delay(50);

            Assert.That(result, Is.EqualTo("ABC"));
            Assert.That(Lines(), Does.Contain("ICalculator.LoadAsync(\"abc\") completed -> \"ABC\""));
            }

        [Test]
        public void RelayCommand_TracesTheStartAndTheEndOfARun()
            {
            var command = new RelayCommand(_ => { });

            command.Execute("Red");

            Assert.That(Lines(), Does.Contain("RelayCommand_TracesTheStartAndTheEndOfARun(\"Red\") started").And.Contain("finished in"));
            }

        [Test]
        public void RelayCommand_TracesAFailureAndKeepsRunning()
            {
            var command = new RelayCommand(_ => throw new InvalidOperationException("boom"));

            Assert.DoesNotThrow(() => command.Execute(null));

            Assert.That(Lines(), Does.Contain("failed: InvalidOperationException: boom"));
            }
        }
    }
#endif
