using IndustrialAgent.Core.Plugins;
using IndustrialAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IndustrialAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeQaController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly CodeSearchPlugin _searchPlugin;
    private readonly ILogger<CodeQaController> _logger;

    public CodeQaController(Kernel kernel, CodeSearchPlugin searchPlugin, ILogger<CodeQaController> logger)
    {
        _kernel = kernel;
        _searchPlugin = searchPlugin;
        _logger = logger;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<CodeQaResponse>> Ask([FromBody] CodeQaRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("问题不能为空");

        var references = await _searchPlugin.GetCodeReferencesAsync(request.Question, 5, ct);

        var systemPrompt = """
            你是工业上位机代码专家。基于检索到的代码片段回答用户问题。

            要求：
            - 回答时引用具体的文件路径和行号
            - 如果问题涉及代码实现，展示相关代码片段
            - 如果检索结果不足以回答，明确说明
            - 使用中文回答
            """;

        var searchResult = await _searchPlugin.SearchCodeAsync(request.Question, 8, request.FilePathFilter, request.KindFilter, ct);

        var userPrompt = $"""
            问题：{request.Question}

            检索到的代码片段：
            {searchResult}
            """;

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 8192,
            Temperature = 0.3
        };

        var response = await chat.GetChatMessageContentAsync(history, settings, _kernel, ct);

        return new CodeQaResponse
        {
            Answer = response.Content ?? string.Empty,
            References = references
        };
    }
}
