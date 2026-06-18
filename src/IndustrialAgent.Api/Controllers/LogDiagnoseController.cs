using IndustrialAgent.Core.Plugins;
using IndustrialAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;

namespace IndustrialAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LogDiagnoseController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly LogDiagnosePlugin _logDiagnosePlugin;
    private readonly ILogger<LogDiagnoseController> _logger;

    public LogDiagnoseController(Kernel kernel, LogDiagnosePlugin logDiagnosePlugin, ILogger<LogDiagnoseController> logger)
    {
        _kernel = kernel;
        _logDiagnosePlugin = logDiagnosePlugin;
        _logger = logger;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<LogDiagnoseResponse>> Analyze([FromBody] LogDiagnoseRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.LogContent))
            return BadRequest("日志内容不能为空");

        var result = await _logDiagnosePlugin.DiagnoseLogAsync(request.LogContent, request.StackTrace, ct);

        return new LogDiagnoseResponse
        {
            RootCause = result,
            CodeLocation = string.Empty,
            FixSuggestion = string.Empty,
            CodeExample = string.Empty
        };
    }
}
