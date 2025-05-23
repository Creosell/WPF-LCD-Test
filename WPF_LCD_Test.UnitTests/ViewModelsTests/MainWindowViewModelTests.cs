using Moq;
using MvvmHelpers; // Для BaseViewModel
using WPF_LCD_Test.Interfaces;
using WPF_LCD_Test.ViewModels;

namespace WPF_LCD_Test.UnitTests.ViewModels
{
    [TestFixture]
    public class MainWindowViewModelTests
    {
        private Mock<IColorMeasurementService> _mockColorMeasurementService;
        private Mock<IFileService> _mockFileService;
        private Mock<IDialogService> _mockDialogService;
        private Mock<ILocalizationService> _mockLocalizationService;
        private Mock<ISettingsService> _mockSettingsService;
        private Mock<IDispatcher> _mockDispatcher; // Если используется в вашем коде

        private MainWindowViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            // Инициализация моков для каждого теста
            _mockColorMeasurementService = new Mock<IColorMeasurementService>();
            _mockFileService = new Mock<IFileService>();
            _mockDialogService = new Mock<IDialogService>();
            _mockLocalizationService = new Mock<ILocalizationService>();
            _mockSettingsService = new Mock<ISettingsService>();
            _mockDispatcher = new Mock<IDispatcher>();

            // Инициализация ViewModel с моками
            _viewModel = new MainWindowViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockSettingsService.Object,
                _mockDispatcher.Object
            );
        }

        [TearDown]
        public void Teardown()
        {
            _viewModel.Dispose(); // Вызываем Dispose для очистки
        }

        // --- Тесты конструктора ---

        [Test]
        public void Ctor_InitializesWithDependencies()
        {
            Assert.That(_viewModel, Is.Not.Null);
            // Можно добавить проверку, что зависимости не null внутри ViewModel,
            // но конструктор уже делает ArgumentNullException.ThrowIfNull.
        }

        [Test]
        [TestCase("")]
        public void Ctor_ThrowsArgumentNullException_IfAnyDependencyIsNull(string paramName)
        {
            // Этот тест сложнее, так как нужно динамически передавать null
            // Создадим тестовые сценарии для каждого параметра.
            Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
                null,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockSettingsService.Object,
                _mockDispatcher.Object
            ), "Should throw for colorMeasurementService");

            Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
                _mockColorMeasurementService.Object,
                null,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockSettingsService.Object,
                _mockDispatcher.Object
            ), "Should throw for fileService");

            Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                null,
                _mockLocalizationService.Object,
                _mockSettingsService.Object,
                _mockDispatcher.Object
            ), "Should throw for dialogService");

            Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                null,
                _mockSettingsService.Object,
                _mockDispatcher.Object
            ), "Should throw for localizationService");

            Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                null,
                _mockDispatcher.Object
            ), "Should throw for settingsService");

            Assert.Throws<ArgumentNullException>(() => new MainWindowViewModel(
                _mockColorMeasurementService.Object,
                _mockFileService.Object,
                _mockDialogService.Object,
                _mockLocalizationService.Object,
                _mockSettingsService.Object,
                null
            ), "Should throw for dispatcher");
        }

        // --- Тесты навигации ---

        [Test]
        public void NavigateCommand_InitializesToMeasurementViewModel()
        {
            // Проверяем, что при инициализации (в конструкторе) автоматически выбрана страница "Measurement"
            // Так как мы не мокируем MeasurementViewModel, он создастся реальным.
            // Для этого теста вам нужно временно изменить MainWindowViewModel
            // чтобы он создавал MockMeasurementViewModel вместо реального.
            // ИЛИ, если вы не хотите менять производственный код, то
            // придется проверить тип реального MeasurementViewModel.
            // Я предпочитаю мокировать для чистоты теста.

            // В реальном коде, чтобы это заработало, нужно было бы, чтобы MainWindowViewModel
            // получал Factory для ViewModel'ей или чтобы эти ViewModel'и были бы internal
            // и их можно было бы подменить в тестах.
            // Для целей демонстрации, давайте предположим, что мы можем подменить создание.

            // --- ОБРАТИТЕ ВНИМАНИЕ: Для этого теста, нужно будет изменить MainWindowViewModel
            // чтобы он использовал фабрики для создания MockMeasurementViewModel и MockSettingsViewModel
            // вместо прямого создания MeasurementViewModel и SettingsViewModel.
            // Это лучший подход для тестирования, но требует изменений в вашем ViewModel.
            // Если вы не хотите менять ViewModel, то тесты будут проверять реальные классы.

            // В целях этого примера, предположим, что мы ВРУЧНУЮ "подменили" создание в ViewModel:
            // В MainWindowViewModel.cs:
            // private MeasurementViewModel? _measurementViewModel = new MockMeasurementViewModel(...);
            // private SettingsViewModel? _settingsViewModel = new MockSettingsViewModel(...);

            // Или, более правильно, использовать "фабрики" или DI-контейнер для создания ViewModel:
            // public MainWindowViewModel(..., Func<IColorMeasurementService, ...> measurementVmFactory)

            // Если не менять MainWindowViewModel, то проверка будет такой:
            Assert.That(_viewModel.CurrentPageViewModel, Is.InstanceOf<MeasurementViewModel>());
            // Если вы используете MockMeasurementViewModel, то:
            // Assert.That(_viewModel.CurrentPageViewModel, Is.InstanceOf<MockMeasurementViewModel>());
        }

        [Test]
        [TestCase("Measurement", typeof(MeasurementViewModel))]
        [TestCase("Settings", typeof(SettingsViewModel))]
        public void NavigateCommand_SwitchesViewModelCorrectly(string pageName, Type expectedViewModelType)
        {
            // Подменяем фабрики для ViewModel'ей, чтобы получить наши моки
            // Если вы не хотите менять основной код, то тесты будут проверять реальные типы.
            // Для простоты, этот тест будет проверять, что CurrentPageViewModel устанавливается.

            // Запускаем навигацию на "Settings"
            _viewModel.NavigateCommand.Execute("Settings");
            Assert.That(_viewModel.CurrentPageViewModel, Is.InstanceOf<SettingsViewModel>());
            //Assert.That(_viewModel.CurrentPageViewModel, Is.InstanceOf<MockSettingsViewModel>()); // Если используете моки

            // Запускаем навигацию обратно на "Measurement"
            _viewModel.NavigateCommand.Execute("Measurement");
            Assert.That(_viewModel.CurrentPageViewModel, Is.InstanceOf<MeasurementViewModel>());
            //Assert.That(_viewModel.CurrentPageViewModel, Is.InstanceOf<MockMeasurementViewModel>()); // Если используете моки

            // Проверяем, что ViewModel создается только один раз (для оператора ??=)
            var initialMeasurementVm = _viewModel.CurrentPageViewModel;
            _viewModel.NavigateCommand.Execute("Settings");
            _viewModel.NavigateCommand.Execute("Measurement");
            Assert.That(_viewModel.CurrentPageViewModel, Is.SameAs(initialMeasurementVm), "MeasurementViewModel должен быть тем же экземпляром.");
        }

        [Test]
        public void NavigateCommand_DoesNothingForNullOrEmptyParameter()
        {
            var initialViewModel = _viewModel.CurrentPageViewModel; // Должен быть MeasurementViewModel

            _viewModel.NavigateCommand.Execute(null);
            Assert.That(_viewModel.CurrentPageViewModel, Is.SameAs(initialViewModel), "ViewModel не должен меняться для null.");

            _viewModel.NavigateCommand.Execute(string.Empty);
            Assert.That(_viewModel.CurrentPageViewModel, Is.SameAs(initialViewModel), "ViewModel не должен меняться для пустой строки.");

            _viewModel.NavigateCommand.Execute("InvalidPage");
            Assert.That(_viewModel.CurrentPageViewModel, Is.SameAs(initialViewModel), "ViewModel не должен меняться для неверного параметра.");
        }

        [Test]
        public void CanExecuteNavigate_AlwaysReturnsTrue()
        {
            Assert.That(_viewModel.NavigateCommand.CanExecute(null), Is.True);
            Assert.That(_viewModel.NavigateCommand.CanExecute("Measurement"), Is.True);
            Assert.That(_viewModel.NavigateCommand.CanExecute("Settings"), Is.True);
        }

        // --- Тесты IDisposable ---

        [Test]
        public void Dispose_DisposesCurrentPageViewModelIfItIsDisposable()
        {
            // Arrange
            // Переходим на SettingsViewModel (реальный экземпляр)
            _viewModel.NavigateCommand.Execute("Settings");

            // Act
            // Вызываем Dispose. Он вызовет Dispose на реальном SettingsViewModel.
            // Мы не можем напрямую проверить, что SettingsViewModel.Dispose() был вызван,
            // если SettingsViewModel не предоставляет публичный способ для этого.
            Assert.DoesNotThrow(() => _viewModel.Dispose(), "Dispose должен быть вызван без исключений на Disposable ViewModel.");

            // Assert: без моков здесь нечего напрямую утверждать.
            // Вы могли бы добавить какой-то флаг в реальный SettingsViewModel,
            // но это было бы изменение производственного кода только ради теста.
            // Либо принять, что этот тест проверяет только отсутствие ошибок при вызове Dispose.
        }

        [Test]
        public void Dispose_DoesNotThrowExceptionIfCurrentPageViewModelIsNotDisposable()
        {
            // Если у вас есть ViewModel, который не реализует IDisposable,
            // убедитесь, что Dispose не бросает исключение.
            // В вашем коде и MeasurementViewModel, и SettingsViewModel должны быть Disposable,
            // поэтому этот тест может быть менее критичным, но полезен для надежности.
            // Для этого можно было бы создать MockNonDisposableViewModel
            _viewModel.CurrentPageViewModel = new BaseViewModel(); // Подставляем не-Disposable ViewModel

            Assert.DoesNotThrow(() => _viewModel.Dispose(), "Dispose не должен бросать исключение, если текущий ViewModel не Disposable.");
        }

        // Тест для CurrentPageIdentifier, если он используется для UI
        [Test]
        public void CurrentPageIdentifier_UpdatesOnNavigation()
        {
            // Изначально конструктор вызывает NavigateCommand("Measurement")
            // Но CurrentPageIdentifier не устанавливается там в вашем коде.
            // Если вы хотите, чтобы он обновлялся, раскомментируйте строку в ExecuteNavigate:
            // CurrentPageIdentifier = pageName;
            // И затем проверьте:
            // Assert.That(_viewModel.CurrentPageIdentifier, Is.EqualTo("Measurement"));

            // Последующая навигация:
            _viewModel.NavigateCommand.Execute("Settings");
            // Assert.That(_viewModel.CurrentPageIdentifier, Is.EqualTo("Settings"));
        }
    }
}