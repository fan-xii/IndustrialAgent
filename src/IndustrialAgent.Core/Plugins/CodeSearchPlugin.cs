using System.ComponentModel;
using System.Text;
using IndustrialAgent.Indexing.VectorStore;
using IndustrialAgent.Shared.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace IndustrialAgent.Core.Plugins;

public sealed class CodeSearchPlugin
{
    private readonly ICodeRepository _repository;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public CodeSearchPlugin(
        ICodeRepository repository,
        ITextEmbeddingGenerationService embeddingService)
    {
        _repository = repository;
        _embeddingService = embeddingService;
    }

    [KernelFunction("search_code")]
    [Description("在已索引的代码库中检索与查询相关的代码片段（类、方法、属性等），返回文件路径、行号和源码内容。用于回答代码定位类问题。")]
    public async Task<string> SearchCodeAsync(
        [Description("自然语言查询，例如：报警逻辑在哪实现、Modbus 通信如何封装")] string query,
        [Description("返回的代码片段数量，默认 8")] int topK = 8,
        [Description("按文件路径过滤（可选）")] string? filePathFilter = null,
        [Description("按符号类型过滤：class/interface/method/property（可选）")] string? kindFilter = null,
        CancellationToken ct = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: ct);
        var results = await _repository.SearchAsync(embedding.ToArray(), topK, filePathFilter, kindFilter, ct);

        if (results.Count == 0)
        {
            return "未找到相关代码片段。请确认代码库已正确索引。";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"找到 {results.Count} 个相关代码片段：\n");
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var filePath = r.Metadata.GetValueOrDefault("filePath", "");
            var startLine = r.Metadata.GetValueOrDefault("startLine", "");
            var endLine = r.Metadata.GetValueOrDefault("endLine", "");
            var kind = r.Metadata.GetValueOrDefault("kind", "");
            var symbol = r.Metadata.GetValueOrDefault("symbolName", "");
            var ns = r.Metadata.GetValueOrDefault("namespace", "");

            sb.AppendLine($"--- 片段 {i + 1} (相关度: {r.Score:F3}) ---");
            sb.AppendLine($"符号: {symbol} ({kind})");
            if (!string.IsNullOrEmpty(ns)) sb.AppendLine($"命名空间: {ns}");
            sb.AppendLine($"文件: {filePath}");
            sb.AppendLine($"行号: {startLine}-{endLine}");
            sb.AppendLine("代码:");
            sb.AppendLine(r.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    [KernelFunction("get_code_references")]
    [Description("仅返回检索到的代码引用列表（含文件路径和行号），不包含完整源码。用于在回答中附上引用。")]
    public async Task<List<SearchResult>> GetCodeReferencesAsync(
        [Description("自然语言查询")] string query,
        [Description("返回数量，默认 5")] int topK = 5,
        CancellationToken ct = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: ct);
        return await _repository.SearchAsync(embedding.ToArray(), topK, null, null, ct);
    }
}
