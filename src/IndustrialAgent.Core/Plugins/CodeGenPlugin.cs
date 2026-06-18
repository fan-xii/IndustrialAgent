using System.ComponentModel;
using System.Text;
using IndustrialAgent.Shared.Architecture;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace IndustrialAgent.Core.Plugins;

public sealed class CodeGenPlugin
{
    private readonly Kernel _kernel;

    public CodeGenPlugin(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction("generate_viewmodel")]
    [Description("根据业务需求生成符合 Prism MVVM 架构规范的 ViewModel 代码。会先检索代码库中同类实现作为参考。")]
    public async Task<string> GenerateViewModelAsync(
        [Description("业务需求描述，例如：设备状态监控，显示温度/压力，支持启停控制")] string requirement,
        [Description("模块名称，例如：DeviceMonitor")] string moduleName,
        CancellationToken ct = default)
    {
        var systemPrompt = ArchitectureRules.BuildCodeGenSystemPrompt("ViewModel");

        var fewShot = await GetFewShotExamplesAsync($"ViewModel {moduleName}", ct);

        var userPrompt = $"""
            模块名称：{moduleName}
            业务需求：{requirement}

            参考代码库中已有的同类实现：
            {fewShot}

            请生成 {moduleName}ViewModel.cs 的完整代码。
            """;

        return await InvokeChatAsync(systemPrompt, userPrompt, ct);
    }

    [KernelFunction("generate_service")]
    [Description("根据业务需求生成服务接口和实现代码，遵循分层架构规范。会先检索代码库中同类实现作为参考。")]
    public async Task<string> GenerateServiceAsync(
        [Description("业务需求描述，例如：Modbus TCP 通信，支持读写保持寄存器")] string requirement,
        [Description("模块名称，例如：ModbusCommunication")] string moduleName,
        CancellationToken ct = default)
    {
        var systemPrompt = ArchitectureRules.BuildCodeGenSystemPrompt("Service")
            .Replace("{ModuleName}", moduleName);

        var fewShot = await GetFewShotExamplesAsync($"Service {moduleName}", ct);

        var userPrompt = $"""
            模块名称：{moduleName}
            业务需求：{requirement}

            参考代码库中已有的同类实现：
            {fewShot}

            请生成 I{moduleName}Service 接口和 {moduleName}Service 实现的完整代码。
            用 === INTERFACE === 和 === IMPLEMENTATION === 分隔两个代码块。
            """;

        return await InvokeChatAsync(systemPrompt, userPrompt, ct);
    }

    [KernelFunction("generate_unit_test")]
    [Description("为指定类生成 xUnit + Moq 单元测试代码。会先检索目标类的实现作为参考。")]
    public async Task<string> GenerateUnitTestAsync(
        [Description("目标类名或全限定名")] string targetClass,
        CancellationToken ct = default)
    {
        var systemPrompt = ArchitectureRules.BuildCodeGenSystemPrompt("UnitTest");

        var fewShot = await GetFewShotExamplesAsync(targetClass, ct);

        var userPrompt = $"""
            目标类：{targetClass}

            目标类的实现代码（供测试参考）：
            {fewShot}

            请为 {targetClass} 生成完整的单元测试代码。
            """;

        return await InvokeChatAsync(systemPrompt, userPrompt, ct);
    }

    private async Task<string> GetFewShotExamplesAsync(string query, CancellationToken ct)
    {
        try
        {
            var searchFn = _kernel.Plugins.GetFunction("CodeSearch", "search_code");
            var result = await _kernel.InvokeAsync(searchFn, new() { ["query"] = query, ["topK"] = 3 }, ct);
            return result.GetValue<string>() ?? "（无参考代码）";
        }
        catch
        {
            return "（代码库未索引或检索失败）";
        }
    }

    private async Task<string> InvokeChatAsync(string systemPrompt, string userPrompt, CancellationToken ct)
    {
        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var settings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
        {
            MaxTokens = 8192,
            Temperature = 0.3
        };

        var response = await chat.GetChatMessageContentAsync(history, settings, _kernel, ct);
        return response.Content ?? string.Empty;
    }
}
