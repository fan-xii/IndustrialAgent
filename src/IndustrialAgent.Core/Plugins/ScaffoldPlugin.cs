using System.ComponentModel;
using System.Text;
using IndustrialAgent.Shared.Architecture;
using IndustrialAgent.Shared.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace IndustrialAgent.Core.Plugins;

public sealed class ScaffoldPlugin
{
    private const string jsonTemplate = """
        {
          "moduleName": "模块名（英文 PascalCase）",
          "summary": "方案概述",
          "files": [
            {
              "path": "相对路径，如 ViewModels/TemperatureViewModel.cs",
              "template": "ViewModel|Service|ServiceInterface|UnitTest|Model|View",
              "content": "该文件的完整代码内容"
            }
          ]
        }
        """;

    private readonly Kernel _kernel;

    public ScaffoldPlugin(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction("plan_scaffold")]
    [Description("根据业务需求规划上位机模块的脚手架方案，输出模块名、目录结构和需要生成的文件清单。")]
    public async Task<string> PlanScaffoldAsync(
        [Description("业务需求描述，例如：温度采集模块，支持 8 通道热电偶采集、实时曲线显示、数据存储")] string requirement,
        CancellationToken ct = default)
    {
        var systemPrompt = $"""
            你是工业上位机架构师。根据业务需求，规划符合 Prism MVVM 架构的模块脚手架方案。

            {ArchitectureRules.PrismMvvmLayers}

            {ArchitectureRules.DirectoryStructure}

            请输出 JSON 格式的方案（不要用 markdown 代码块包裹，直接输出纯 JSON）：
            {jsonTemplate}

            要求：
            - 每个文件都要有完整的代码内容，不要省略
            - 遵循命名约定和分层规范
            - 包含 DI 注册所需的代码片段
            """;

        var userPrompt = $"业务需求：{requirement}";

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
