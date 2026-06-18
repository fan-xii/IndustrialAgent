using IndustrialAgent.Core.Plugins;
using IndustrialAgent.Indexing.Docs;
using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace IndustrialAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocRagController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly DocSearchPlugin _docSearchPlugin;
    private readonly DocIndexer _docIndexer;
    private readonly TextExtractorFactory _extractorFactory;
    private readonly WorkspaceOptions _workspaceOptions;
    private readonly ILogger<DocRagController> _logger;

    public DocRagController(
        Kernel kernel,
        DocSearchPlugin docSearchPlugin,
        DocIndexer docIndexer,
        TextExtractorFactory extractorFactory,
        IOptions<WorkspaceOptions> workspaceOptions,
        ILogger<DocRagController> logger)
    {
        _kernel = kernel;
        _docSearchPlugin = docSearchPlugin;
        _docIndexer = docIndexer;
        _extractorFactory = extractorFactory;
        _workspaceOptions = workspaceOptions.Value;
        _logger = logger;
    }

    [HttpPost("upload")]
    public async Task<ActionResult> Upload([FromForm] IFormFile file, [FromForm] string protocol, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请上传文件");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_workspaceOptions.AllowedUploadExtensions.Contains(ext))
            return BadRequest($"不支持的文件类型：{ext}");

        var tempPath = Path.Combine(Path.GetTempPath(), $"industrial_agent_{Guid.NewGuid():N}{ext}");
        await using (var stream = System.IO.File.Create(tempPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        try
        {
            var chunkCount = await _docIndexer.IndexAsync(tempPath, protocol, ct);
            return Ok(new { docName = file.FileName, protocol, chunks = chunkCount });
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    [HttpPost("ask")]
    public async Task<ActionResult<DocRagResponse>> Ask([FromBody] DocRagRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
            return BadRequest("问题不能为空");

        var searchResult = await _docSearchPlugin.SearchDocsAsync(request.Question, request.Protocol, 6, ct);

        var systemPrompt = """
            你是工业协议文档专家。基于检索到的协议文档片段回答用户问题。

            支持的协议：Modbus、SECS/GEM、S7、EtherNet/IP

            要求：
            - 准确回答协议指令、寄存器地址、报文格式等技术问题
            - 引用文档名和页码
            - 如果检索结果不足，明确说明
            - 使用中文回答
            """;

        var userPrompt = $"""
            问题：{request.Question}

            检索到的协议文档片段：
            {searchResult}
            """;

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var settings = new OpenAIPromptExecutionSettings
        {
            MaxTokens = 8192,
            Temperature = 0.2
        };

        var response = await chat.GetChatMessageContentAsync(history, settings, _kernel, ct);

        return new DocRagResponse
        {
            Answer = response.Content ?? string.Empty
        };
    }
}
