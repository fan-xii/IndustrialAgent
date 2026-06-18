using System.ComponentModel;
using System.Text;
using IndustrialAgent.Indexing.VectorStore;
using IndustrialAgent.Shared.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace IndustrialAgent.Core.Plugins;

public sealed class DocSearchPlugin
{
    private readonly IDocRepository _repository;
    private readonly ITextEmbeddingGenerationService _embeddingService;

    public DocSearchPlugin(
        IDocRepository repository,
        ITextEmbeddingGenerationService embeddingService)
    {
        _repository = repository;
        _embeddingService = embeddingService;
    }

    [KernelFunction("search_docs")]
    [Description("在已索引的工业协议文档（Modbus、SECS/GEM、S7、EtherNet/IP 等）中检索相关内容，返回文档名、页码和文本片段。用于回答协议指令、寄存器地址、报文格式等技术问题。")]
    public async Task<string> SearchDocsAsync(
        [Description("自然语言查询，例如：读保持寄存器功能码 03 的报文格式")] string query,
        [Description("按协议类型过滤：Modbus/SECS_GEM/S7/EtherNetIP（可选）")] string? protocol = null,
        [Description("返回的文档片段数量，默认 6")] int topK = 6,
        CancellationToken ct = default)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken: ct);
        var results = await _repository.SearchAsync(embedding.ToArray(), topK, protocol, ct);

        if (results.Count == 0)
        {
            return "未找到相关协议文档内容。请确认文档已上传并索引。";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"找到 {results.Count} 个相关文档片段：\n");
        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            var docName = r.Metadata.GetValueOrDefault("docName", "");
            var page = r.Metadata.GetValueOrDefault("page", "");
            var section = r.Metadata.GetValueOrDefault("section", "");
            var proto = r.Metadata.GetValueOrDefault("protocol", "");

            sb.AppendLine($"--- 片段 {i + 1} (相关度: {r.Score:F3}) ---");
            sb.AppendLine($"文档: {docName}");
            if (!string.IsNullOrEmpty(proto)) sb.AppendLine($"协议: {proto}");
            if (!string.IsNullOrEmpty(page)) sb.AppendLine($"页码: {page}");
            if (!string.IsNullOrEmpty(section)) sb.AppendLine($"章节: {section}");
            sb.AppendLine("内容:");
            sb.AppendLine(r.Content);
            sb.AppendLine();
        }

        return sb.ToString();
    }
}
