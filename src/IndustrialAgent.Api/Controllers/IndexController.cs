using IndustrialAgent.Indexing.Code;
using IndustrialAgent.Indexing.VectorStore;
using IndustrialAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{
    private readonly RoslynCodeParser _parser;
    private readonly CodeIndexer _indexer;
    private readonly ICodeRepository _repository;
    private readonly ILogger<IndexController> _logger;

    public IndexController(RoslynCodeParser parser, CodeIndexer indexer, ICodeRepository repository, ILogger<IndexController> logger)
    {
        _parser = parser;
        _indexer = indexer;
        _repository = repository;
        _logger = logger;
    }

    [HttpPost("project")]
    public async Task<ActionResult<IndexProjectResponse>> IndexProject([FromBody] IndexProjectRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SolutionOrProjectPath))
            return BadRequest("解决方案或项目路径不能为空");
        if (!System.IO.File.Exists(request.SolutionOrProjectPath))
            return BadRequest($"文件不存在：{request.SolutionOrProjectPath}");

        _logger.LogInformation("开始索引：{Path}", request.SolutionOrProjectPath);

        var parsed = await _parser.ParseAsync(request.SolutionOrProjectPath, ct);
        var response = await _indexer.IndexAsync(parsed, request.ForceReindex, ct);

        _logger.LogInformation("索引完成：{Chunks} 个代码片段，{Errors} 个错误",
            response.TotalChunks, response.Errors.Count);

        return response;
    }

    [HttpGet("status")]
    public async Task<ActionResult<object>> GetStatus(CancellationToken ct)
    {
        try
        {
            var count = await _repository.CountAsync(ct);
            return new { totalChunks = count, status = "ready" };
        }
        catch
        {
            return new { totalChunks = 0, status = "not_initialized" };
        }
    }
}
