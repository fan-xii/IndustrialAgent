using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Shared.Models;
using IndustrialAgent.Indexing.VectorStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Embeddings;

namespace IndustrialAgent.Indexing.Code;

public sealed class CodeIndexer
{
    private readonly ICodeRepository _repository;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly IndexingOptions _options;
    private readonly ILogger<CodeIndexer> _logger;

    public CodeIndexer(
        ICodeRepository repository,
        ITextEmbeddingGenerationService embeddingService,
        IOptions<IndexingOptions> options,
        ILogger<CodeIndexer> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IndexProjectResponse> IndexAsync(ParsedSolution parsed, bool forceReindex, CancellationToken ct = default)
    {
        var response = new IndexProjectResponse();
        var allChunks = new List<CodeChunk>();

        foreach (var project in parsed.Projects)
        {
            foreach (var doc in project.Documents)
            {
                try
                {
                    var slicer = new CodeSlicer();
                    var chunks = await slicer.SliceAsync(doc, project.Name, ct);
                    allChunks.AddRange(chunks);
                    response.TotalDocuments++;
                }
                catch (Exception ex)
                {
                    response.FailedDocuments++;
                    response.Errors.Add($"{doc.Name}: {ex.Message}");
                    _logger.LogWarning(ex, "切片文档失败：{Doc}", doc.Name);
                }
            }
        }

        response.TotalChunks = allChunks.Count;

        var batches = allChunks.Chunk(_options.EmbeddingBatchSize);
        foreach (var batch in batches)
        {
            try
            {
                var texts = batch.Select(c => BuildEmbeddingText(c)).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
                for (int i = 0; i < batch.Length; i++)
                {
                    batch[i].Vector = embeddings[i].ToArray();
                }
                await _repository.UpsertAsync(batch, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向量化/入库批次失败");
                response.Errors.Add($"批次入库失败：{ex.Message}");
            }
        }

        return response;
    }

    private static string BuildEmbeddingText(CodeChunk chunk)
    {
        return $"""
            符号：{chunk.QualifiedName}
            类型：{chunk.Kind}
            文件：{chunk.FilePath}
            命名空间：{chunk.Namespace}
            
            {chunk.Content}
            """;
    }
}
