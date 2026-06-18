using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using IndustrialAgent.Shared.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace IndustrialAgent.Core.Plugins;

public sealed class LogDiagnosePlugin
{
    private readonly Kernel _kernel;

    public LogDiagnosePlugin(Kernel kernel)
    {
        _kernel = kernel;
    }

    [KernelFunction("diagnose_log")]
    [Description("分析程序运行日志和异常堆栈，自动定位根因、找到对应代码位置、给出修复方案和代码示例。")]
    public async Task<string> DiagnoseLogAsync(
        [Description("程序运行日志内容")] string logContent,
        [Description("异常堆栈（可选，若日志中已包含可留空）")] string? stackTrace = null,
        CancellationToken ct = default)
    {
        var fullLog = string.IsNullOrEmpty(stackTrace) ? logContent : $"{logContent}\n\n堆栈跟踪:\n{stackTrace}";

        var extractedSymbols = ExtractSymbolsFromLog(fullLog);
        var codeContext = await GetCodeContextAsync(extractedSymbols, ct);

        var systemPrompt = """
            你是工业上位机故障排查专家。请分析日志和异常堆栈，按以下四段式输出诊断报告：

            ## 根因分析
            分析异常的根本原因。

            ## 代码定位
            指出出错的文件路径和行号（基于检索到的代码片段）。

            ## 修复方案
            给出具体的修复建议。

            ## 代码示例
            给出修复后的代码示例（使用 ```csharp 代码块）。

            要求：
            - 基于检索到的实际代码片段进行分析，不要臆测
            - 如果无法定位代码，明确说明
            - 修复方案要具体可执行
            """;

        var userPrompt = $"""
            日志内容：
            {fullLog}

            从堆栈中提取的符号信息：{string.Join(", ", extractedSymbols)}

            检索到的相关代码片段：
            {codeContext}
            """;

        var chat = _kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage(systemPrompt);
        history.AddUserMessage(userPrompt);

        var settings = new Microsoft.SemanticKernel.Connectors.OpenAI.OpenAIPromptExecutionSettings
        {
            MaxTokens = 8192,
            Temperature = 0.2
        };

        var response = await chat.GetChatMessageContentAsync(history, settings, _kernel, ct);
        return response.Content ?? string.Empty;
    }

    private static List<string> ExtractSymbolsFromLog(string log)
    {
        var symbols = new HashSet<string>();

        var typeMethodPattern = @"(\w+)\.(\w+)\(([^)]*)\)";
        foreach (Match m in Regex.Matches(log, typeMethodPattern))
        {
            symbols.Add($"{m.Groups[1].Value}.{m.Groups[2].Value}");
        }

        var filePathPattern = @"在\s+([\w.\\/-]+\.cs):行号\s*(\d+)";
        foreach (Match m in Regex.Matches(log, filePathPattern))
        {
            symbols.Add(m.Groups[1].Value);
        }

        var exceptionPattern = @"(\w+Exception)";
        foreach (Match m in Regex.Matches(log, exceptionPattern))
        {
            symbols.Add(m.Groups[1].Value);
        }

        return symbols.Take(5).ToList();
    }

    private async Task<string> GetCodeContextAsync(List<string> symbols, CancellationToken ct)
    {
        if (symbols.Count == 0) return "（未从日志中提取到符号信息）";

        var sb = new StringBuilder();
        try
        {
            var searchFn = _kernel.Plugins.GetFunction("CodeSearch", "search_code");
            foreach (var symbol in symbols)
            {
                var result = await _kernel.InvokeAsync(searchFn, new() { ["query"] = symbol, ["topK"] = 3 }, ct);
                sb.AppendLine($"查询符号 {symbol}:");
                sb.AppendLine(result.GetValue<string>() ?? "");
                sb.AppendLine();
            }
        }
        catch
        {
            sb.AppendLine("（代码检索失败，可能代码库未索引）");
        }
        return sb.ToString();
    }
}
