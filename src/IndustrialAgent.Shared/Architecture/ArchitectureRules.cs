namespace IndustrialAgent.Shared.Architecture;

public static class ArchitectureRules
{
    public const string PrismMvvmLayers = """
        项目分层规范（Prism MVVM）：
        - Views/         : XAML 视图，仅负责 UI 绑定，不包含业务逻辑
        - ViewModels/    : 视图模型，继承 BindableBase，实现 INotifyPropertyChanged
        - Services/      : 业务服务实现，实现对应 Interfaces 中的接口
        - Interfaces/    : 服务接口定义，命名以 I 开头（如 IDeviceService）
        - Models/        : 数据模型/实体类，POCO 或实现 IEditableObject

        命名约定：
        - ViewModel 命名：{Module}ViewModel，如 TemperatureViewModel
        - Service 接口命名：I{Module}Service，如 ITemperatureService
        - Service 实现命名：{Module}Service，如 TemperatureService
        - 命令属性命名：{Action}Command，如 StartAcquisitionCommand
        - 属性命名：PascalCase，集合类型使用复数

        依赖注入约定：
        - 所有服务在 App.xaml.cs 的 RegisterTypes 中注册
        - ViewModel 通过容器解析，使用 DialogService 导航
        - 服务注册：containerRegistry.RegisterSingleton<IService, Service>()

        通信模式：
        - 模块间通信使用 IEventAggregator 发布/订阅事件
        - 事件类命名：{Domain}Event，如 DeviceStatusChangedEvent
        - 命令使用 DelegateCommand 或 DelegateCommand<T>
        """;

    public const string CodeGenConventions = """
        代码生成规范：
        1. ViewModel 必须继承 BindableBase，构造函数注入服务依赖
        2. 公开属性使用 SetProperty(ref _field, value) 触发通知
        3. 命令使用 DelegateCommand 封装，支持 CanExecute
        4. 异步方法使用 Async 后缀，返回 Task 或 Task<T>
        5. 服务实现必须实现对应接口，方法添加 XML 注释
        6. 单元测试使用 xUnit + Moq，测试类命名 {Class}Tests
        7. 所有公共成员添加 /// <summary> XML 文档注释
        8. 使用 var 声明局部变量，显式类型声明字段和属性
        9. 使用表达式体成员简化简单属性和方法
        10. 异常处理：捕获特定异常，记录日志后向上抛出
        """;

    public const string DirectoryStructure = """
        标准模块目录结构：
        {Module}/
        ├── ViewModels/
        │   └── {Module}ViewModel.cs
        ├── Views/
        │   └── {Module}View.xaml
        ├── Services/
        │   └── {Module}Service.cs
        ├── Interfaces/
        │   └── I{Module}Service.cs
        ├── Models/
        │   └── {Module}Model.cs
        └── Tests/
            └── {Module}ViewModelTests.cs
        """;

    public static string BuildCodeGenSystemPrompt(string codeType) => codeType switch
    {
        "ViewModel" => $"""
            你是工业上位机代码生成专家，严格遵循 Prism MVVM 架构规范。
            
            {PrismMvvmLayers}
            
            {CodeGenConventions}
            
            生成要求：
            - 生成符合 Prism 规范的 ViewModel 类
            - 继承 BindableBase
            - 构造函数注入所需服务
            - 包含必要的属性和命令
            - 添加完整的 XML 文档注释
            - 仅输出 C# 代码，不要额外解释
            """,
        "Service" => $"""
            你是工业上位机代码生成专家，严格遵循分层架构规范。
            
            {PrismMvvmLayers}
            
            {CodeGenConventions}
            
            生成要求：
            - 同时生成接口（I[ModuleName]Service）和实现（[ModuleName]Service）
            - 接口和实现分两个代码块输出，用 === INTERFACE === 和 === IMPLEMENTATION === 分隔
            - 实现类必须实现接口
            - 方法添加 XML 注释
            - 仅输出 C# 代码，不要额外解释
            """,
        "UnitTest" => $"""
            你是工业上位机测试代码生成专家。
            
            {CodeGenConventions}
            
            生成要求：
            - 使用 xUnit + Moq 框架
            - 测试类命名：[TargetClass]Tests
            - 每个公共方法至少 3 个测试用例（正常/边界/异常）
            - 使用 AAA 模式（Arrange-Act-Assert）
            - 仅输出 C# 代码，不要额外解释
            """,
        _ => PrismMvvmLayers
    };
}
