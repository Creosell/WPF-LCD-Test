using WPF_LCD_Test.Commands; // Убедитесь, что это правильное пространство имен

namespace WPF_LCD_Test.UnitTests.Commands
{
    [TestFixture]
    public class RelayCommandTests
    {
        // Тесты для конструкторов

        [Test]
        public void Ctor_Action_NullBehavior()
        {
            // Parameterless Action overload wraps delegate and does not throw on null
            Assert.DoesNotThrow(() => new RelayCommand((Action)null));
            // Parameterized Action<object> overload does throw on null
            Assert.Throws<ArgumentNullException>(() => new RelayCommand((Action<object>)null));
        }

        [Test]
        public void Ctor_FuncTask_NullBehavior()
        {
            // Parameterless Func<Task> overload wraps delegate and does not throw on null
            Assert.DoesNotThrow(() => new RelayCommand((Func<Task>)null));
            // Parameterized Func<object, Task> overload throws on null
            Assert.Throws<ArgumentNullException>(() => new RelayCommand((Func<object, Task>)null));
        }

        // Тесты для синхронных команд

        [Test]
        public void CanExecute_SyncCommand_WithoutCanExecuteDelegate_ReturnsTrue()
        {
            var command = new RelayCommand(() => { });
            Assert.That(command.CanExecute(null), Is.True);
            Assert.That(command.CanExecute("param"), Is.True);
        }

        [Test]
        public void CanExecute_SyncCommand_WithCanExecuteDelegate_ReturnsDelegateResult()
        {
            bool canExecuteResult = false;
            var command = new RelayCommand(() => { }, () => canExecuteResult);

            canExecuteResult = true;
            Assert.That(command.CanExecute(null), Is.True);

            canExecuteResult = false;
            Assert.That(command.CanExecute(null), Is.False);
        }

        [Test]
        public void CanExecute_SyncCommand_WithCanExecuteDelegate_ParameterPassedCorrectly()
        {
            string receivedParam = null;
            var command = new RelayCommand((p) => { }, (p) => { receivedParam = p as string; return true; });

            command.CanExecute("testParam");
            Assert.That(receivedParam, Is.EqualTo("testParam"));
        }

        [Test]
        public void Execute_SyncCommand_CallsExecuteDelegateWhenCanExecuteTrue()
        {
            bool executeCalled = false;
            var command = new RelayCommand(() => { executeCalled = true; }, () => true);

            command.Execute(null);
            Assert.That(executeCalled, Is.True);
        }

        [Test]
        public void Execute_SyncCommand_DoesNotCallExecuteDelegateWhenCanExecuteFalse()
        {
            bool executeCalled = false;
            var command = new RelayCommand(() => { executeCalled = true; }, () => false);

            command.Execute(null);
            Assert.That(executeCalled, Is.False);
        }

        [Test]
        public void Execute_SyncCommand_ParameterPassedCorrectly()
        {
            string receivedParam = null;
            var command = new RelayCommand((p) => { receivedParam = p as string; });

            command.Execute("syncParam");
            Assert.That(receivedParam, Is.EqualTo("syncParam"));
        }

        // Тесты для асинхронных команд

        [Test]
        public async Task CanExecute_AsyncCommand_WithoutCanExecuteDelegate_ReturnsTrueWhenNotExecuting()
        {
            var tcs = new TaskCompletionSource<bool>();
            var command = new RelayCommand(async () => await tcs.Task);

            Assert.That(command.CanExecute(null), Is.True); // Не выполняется

            // Запускаем команду, чтобы она перешла в состояние выполнения
            command.Execute(null);
            Assert.That(command.CanExecute(null), Is.False); // Выполняется

            tcs.SetResult(true); // Завершаем команду
            await Task.Delay(10); // Даем время на обновление состояния
            Assert.That(command.CanExecute(null), Is.True); // Не выполняется
        }

        [Test]
        public async Task CanExecute_AsyncCommand_WithCanExecuteDelegate_ReturnsDelegateResultAndNotExecuting()
        {
            bool baseCanExecuteResult = true;
            var tcs = new TaskCompletionSource<bool>();
            var command = new RelayCommand(async () => await tcs.Task, () => baseCanExecuteResult);

            Assert.That(command.CanExecute(null), Is.True); // True && NotExecuting

            baseCanExecuteResult = false;
            Assert.That(command.CanExecute(null), Is.False); // False && NotExecuting

            baseCanExecuteResult = true;
            command.Execute(null); // Запускаем
            Assert.That(command.CanExecute(null), Is.False); // True && Executing (т.е. false)

            tcs.SetResult(true);
            await Task.Delay(10);
            Assert.That(command.CanExecute(null), Is.True); // True && NotExecuting
        }

        [Test]
        public async Task Execute_AsyncCommand_CallsExecuteAsyncDelegate()
        {
            bool executeCalled = false;
            var command = new RelayCommand(async () => { await Task.Delay(1); executeCalled = true; });

            command.Execute(null);
            await Task.Delay(100); // Даем время на выполнение асинхронной операции

            Assert.That(executeCalled, Is.True);
        }

        [Test]
        public async Task Execute_AsyncCommand_ParameterPassedCorrectly()
        {
            string receivedParam = null;
            var command = new RelayCommand(async (p) => { await Task.Delay(1); receivedParam = p as string; });

            command.Execute("asyncParam");
            await Task.Delay(100);

            Assert.That(receivedParam, Is.EqualTo("asyncParam"));
        }

        [Test]
        public async Task Execute_AsyncCommand_IsExecutingFlagIsManaged()
        {
            var tcs = new TaskCompletionSource<bool>();
            var command = new RelayCommand(async () => { await tcs.Task; });

            Assert.That(command.CanExecute(null), Is.True); // Изначально true

            command.Execute(null);
            Assert.That(command.CanExecute(null), Is.False); // Должно быть false во время выполнения

            tcs.SetResult(true); // Завершаем команду
            await Task.Delay(10); // Даем время на обновление состояния

            Assert.That(command.CanExecute(null), Is.True); // Должно быть true после завершения
        }

        [Test]
        public async Task Execute_AsyncCommand_ExceptionIsCaughtAndDoesNotCrashApp()
        {
            var command = new RelayCommand(async () => { await Task.Delay(1); throw new InvalidOperationException("Test exception"); });

            Assert.DoesNotThrow(() => command.Execute(null));

            await Task.Delay(100);

            Assert.That(command.CanExecute(null), Is.True);
        }

        // Тесты для RaiseCanExecuteChanged

        [Test]
        public void RaiseCanExecuteChanged_InvokesCanExecuteChangedEvent()
        {
            var command = new RelayCommand(() => { });
            bool eventFired = false;
            command.CanExecuteChanged += (sender, args) => eventFired = true;

            command.RaiseCanExecuteChanged();
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public async Task RaiseCanExecuteChanged_InvokedByExecuteForAsyncCommand()
        {
            var tcs = new TaskCompletionSource<bool>();
            var command = new RelayCommand(async () => { await tcs.Task; });

            int eventCount = 0;
            command.CanExecuteChanged += (sender, args) => eventCount++;

            command.Execute(null); // Должен вызвать 1 раз (для установки _isExecuting = true)
            Assert.That(eventCount, Is.EqualTo(1));

            tcs.SetResult(true);
            await Task.Delay(10); // Даем время на выполнение finally блока

            Assert.That(eventCount, Is.EqualTo(2)); // Должен вызвать 2й раз (для установки _isExecuting = false)
        }
    }
}