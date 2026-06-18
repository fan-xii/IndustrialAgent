using IndustrialAgent.Core.Plugins;
using IndustrialAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

namespace IndustrialAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CodeGenController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly CodeGenPlugin _codeGenPlugin;
    private readonly ILogger<CodeGenController> _logger;

    public CodeGenController(Kernel kernel, CodeGenPlugin codeGenPlugin, ILogger<CodeGenController> logger)
    {
        _kernel = kernel;
        _codeGenPlugin = codeGenPlugin;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<CodeGenResponse>> Generate([FromBody] CodeGenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Requirement))
            return BadRequest("需求描述不能为空");
        if (string.IsNullOrWhiteSpace(request.ModuleName))
            return BadRequest("模块名不能为空");

        string code;
        switch (request.Type)
        {
            case "ViewModel":
                code = await _codeGenPlugin.GenerateViewModelAsync(request.Requirement, request.ModuleName, ct);
                break;
            case "Service":
                code = await _codeGenPlugin.GenerateServiceAsync(request.Requirement, request.ModuleName, ct);
                break;
            case "UnitTest":
                code = await _codeGenPlugin.GenerateUnitTestAsync(request.Requirement, ct);
                break;
            default:
                return BadRequest($"不支持的代码类型：{request.Type}。支持：ViewModel, Service, UnitTest");
        }

        var targetPath = request.Type switch
        {
            "ViewModel" => $"ViewModels/{request.ModuleName}ViewModel.cs",
            "Service" => $"Services/{request.ModuleName}Service.cs",
            "UnitTest" => $"Tests/{request.ModuleName}Tests.cs",
            _ => $"{request.ModuleName}.cs"
        };

        var writtenToDisk = false;
        if (request.WriteToDisk && !string.IsNullOrEmpty(request.ModuleName))
        {
            _logger.LogInformation("代码生成写入磁盘已请求，但需要前端确认目标目录");
        }

        return new CodeGenResponse
        {
            Code = code,
            TargetPath = targetPath,
            Language = "csharp",
            WrittenToDisk = writtenToDisk
        };
    }
}
