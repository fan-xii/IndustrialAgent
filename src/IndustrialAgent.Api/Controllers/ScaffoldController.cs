using IndustrialAgent.Core.Plugins;
using IndustrialAgent.Scaffolding;
using IndustrialAgent.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using System.Text.Json;

namespace IndustrialAgent.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScaffoldController : ControllerBase
{
    private readonly Kernel _kernel;
    private readonly ScaffoldPlugin _scaffoldPlugin;
    private readonly ScaffoldGenerator _scaffoldGenerator;
    private readonly ILogger<ScaffoldController> _logger;

    public ScaffoldController(
        Kernel kernel,
        ScaffoldPlugin scaffoldPlugin,
        ScaffoldGenerator scaffoldGenerator,
        ILogger<ScaffoldController> logger)
    {
        _kernel = kernel;
        _scaffoldPlugin = scaffoldPlugin;
        _scaffoldGenerator = scaffoldGenerator;
        _logger = logger;
    }

    [HttpPost("plan")]
    public async Task<ActionResult<ScaffoldPlanResponse>> Plan([FromBody] ScaffoldPlanRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Requirement))
            return BadRequest("需求描述不能为空");

        var jsonResult = await _scaffoldPlugin.PlanScaffoldAsync(request.Requirement, ct);

        try
        {
            var json = ExtractJson(jsonResult);
            var plan = JsonSerializer.Deserialize<ScaffoldPlanResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (plan is null)
            {
                var moduleName = ScaffoldGenerator.ExtractModuleNameFromJson(json) ?? "NewModule";
                plan = _scaffoldGenerator.GeneratePlan(moduleName, $"IndustrialAgent.{moduleName}", jsonResult);
            }

            return plan;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "解析 GLM 脚手架方案失败，使用默认模板");
            var moduleName = ScaffoldGenerator.ExtractModuleNameFromJson(jsonResult) ?? "NewModule";
            return _scaffoldGenerator.GeneratePlan(moduleName, $"IndustrialAgent.{moduleName}", jsonResult);
        }
    }

    [HttpPost("plan-template")]
    public ActionResult<ScaffoldPlanResponse> PlanTemplate([FromBody] ScaffoldPlanRequest request)
    {
        var moduleName = string.IsNullOrWhiteSpace(request.Requirement)
            ? "NewModule"
            : ExtractModuleName(request.Requirement);
        return _scaffoldGenerator.GeneratePlan(moduleName, $"IndustrialAgent.{moduleName}", request.Requirement);
    }

    [HttpPost("execute")]
    public ActionResult<ScaffoldExecuteResponse> Execute([FromBody] ScaffoldExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetDir))
            return BadRequest("目标目录不能为空");
        if (!Directory.Exists(request.TargetDir))
            return BadRequest($"目标目录不存在：{request.TargetDir}");

        return _scaffoldGenerator.Execute(request.Plan, request.TargetDir);
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return text.Substring(start, end - start + 1);
        }
        return text;
    }

    private static string ExtractModuleName(string requirement)
    {
        var words = requirement.Split(new[] { ' ', '，', '、', '：' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (word.Contains("模块") || word.Contains("Module"))
            {
                var name = word.Replace("模块", "").Replace("Module", "").Trim();
                if (!string.IsNullOrEmpty(name) && char.IsLetter(name[0]))
                {
                    return char.ToUpperInvariant(name[0]) + name[1..];
                }
            }
        }
        return "NewModule";
    }
}
