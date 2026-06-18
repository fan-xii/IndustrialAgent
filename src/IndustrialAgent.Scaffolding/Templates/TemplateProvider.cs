namespace IndustrialAgent.Scaffolding.Templates;

public static class TemplateProvider
{
    public const string ViewModelTemplate = """
        using Prism.Commands;
        using Prism.Mvvm;
        using System.Windows.Input;

        namespace {{Namespace}}.ViewModels;

        /// <summary>
        /// {{ModuleName}} 视图模型。
        /// </summary>
        public class {{ModuleName}}ViewModel : BindableBase
        {
            private readonly I{{ModuleName}}Service _{{ModuleNameCamel}}Service;

            /// <summary>
            /// 初始化 {{ModuleName}}ViewModel 的新实例。
            /// </summary>
            /// <param name="{{ModuleNameCamel}}Service">{{ModuleName}} 服务。</param>
            public {{ModuleName}}ViewModel(I{{ModuleName}}Service {{ModuleNameCamel}}Service)
            {
                _{{ModuleNameCamel}}Service = {{ModuleNameCamel}}Service ?? throw new ArgumentNullException(nameof({{ModuleNameCamel}}Service));
                StartCommand = new DelegateCommand(ExecuteStart, CanExecuteStart);
                StopCommand = new DelegateCommand(ExecuteStop, CanExecuteStop);
            }

            private string _status = "Idle";
            /// <summary>
            /// 获取或设置状态。
            /// </summary>
            public string Status
            {
                get => _status;
                set => SetProperty(ref _status, value);
            }

            /// <summary>
            /// 启动命令。
            /// </summary>
            public ICommand StartCommand { get; }

            /// <summary>
            /// 停止命令。
            /// </summary>
            public ICommand StopCommand { get; }

            private void ExecuteStart()
            {
                Status = "Running";
            }

            private bool CanExecuteStart() => Status != "Running";

            private void ExecuteStop()
            {
                Status = "Stopped";
            }

            private bool CanExecuteStop() => Status == "Running";
        }
        """;

    public const string ServiceInterfaceTemplate = """
        namespace {{Namespace}}.Interfaces;

        /// <summary>
        /// {{ModuleName}} 服务接口。
        /// </summary>
        public interface I{{ModuleName}}Service
        {
            /// <summary>
            /// 启动服务。
            /// </summary>
            /// <param name="cancellationToken">取消令牌。</param>
            /// <returns>表示异步操作的任务。</returns>
            Task StartAsync(CancellationToken cancellationToken = default);

            /// <summary>
            /// 停止服务。
            /// </summary>
            /// <param name="cancellationToken">取消令牌。</param>
            /// <returns>表示异步操作的任务。</returns>
            Task StopAsync(CancellationToken cancellationToken = default);

            /// <summary>
            /// 获取当前状态。
            /// </summary>
            /// <returns>状态字符串。</returns>
            string GetStatus();
        }
        """;

    public const string ServiceImplTemplate = """
        using Microsoft.Extensions.Logging;
        using {{Namespace}}.Interfaces;

        namespace {{Namespace}}.Services;

        /// <summary>
        /// {{ModuleName}} 服务实现。
        /// </summary>
        public class {{ModuleName}}Service : I{{ModuleName}}Service
        {
            private readonly ILogger<{{ModuleName}}Service> _logger;
            private bool _isRunning;

            /// <summary>
            /// 初始化 {{ModuleName}}Service 的新实例。
            /// </summary>
            /// <param name="logger">日志记录器。</param>
            public {{ModuleName}}Service(ILogger<{{ModuleName}}Service> logger)
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            /// <inheritdoc/>
            public async Task StartAsync(CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("{{ModuleName}} 服务启动");
                _isRunning = true;
                await Task.CompletedTask;
            }

            /// <inheritdoc/>
            public async Task StopAsync(CancellationToken cancellationToken = default)
            {
                _logger.LogInformation("{{ModuleName}} 服务停止");
                _isRunning = false;
                await Task.CompletedTask;
            }

            /// <inheritdoc/>
            public string GetStatus() => _isRunning ? "Running" : "Stopped";
        }
        """;

    public const string UnitTestTemplate = """
        using Microsoft.Extensions.Logging.Abstractions;
        using Moq;
        using Xunit;
        using {{Namespace}}.Services;
        using {{Namespace}}.ViewModels;

        namespace {{Namespace}}.Tests;

        /// <summary>
        /// {{ModuleName}}ViewModel 单元测试。
        /// </summary>
        public class {{ModuleName}}ViewModelTests
        {
            private readonly Mock<global::{{Namespace}}.Interfaces.I{{ModuleName}}Service> _mockService;
            private readonly {{ModuleName}}ViewModel _viewModel;

            public {{ModuleName}}ViewModelTests()
            {
                _mockService = new Mock<global::{{Namespace}}.Interfaces.I{{ModuleName}}Service>();
                _viewModel = new {{ModuleName}}ViewModel(_mockService.Object);
            }

            [Fact]
            public void Constructor_WithNullService_ThrowsArgumentNullException()
            {
                Assert.Throws<ArgumentNullException>(() => new {{ModuleName}}ViewModel(null!));
            }

            [Fact]
            public void InitialStatus_IsIdle()
            {
                Assert.Equal("Idle", _viewModel.Status);
            }

            [Fact]
            public void StartCommand_WhenExecuted_SetsStatusToRunning()
            {
                _viewModel.StartCommand.Execute(null);
                Assert.Equal("Running", _viewModel.Status);
            }
        }
        """;

    public static string GetTemplate(string templateName) => templateName switch
    {
        "ViewModel" => ViewModelTemplate,
        "Service" => ServiceImplTemplate,
        "ServiceInterface" => ServiceInterfaceTemplate,
        "UnitTest" => UnitTestTemplate,
        _ => throw new ArgumentException($"未知模板：{templateName}", nameof(templateName))
    };
}
