using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Shared.Models;
using IndustrialAgent.Indexing.Docs;
using IndustrialAgent.Indexing.VectorStore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.Embeddings;

namespace IndustrialAgent.Indexing.Docs;

public sealed class DocIndexer
{
    private readonly IDocRepository _repository;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly TextExtractorFactory _extractorFactory;
    private readonly TextChunker _chunker;
    private readonly IndexingOptions _options;
    private readonly ILogger<DocIndexer> _logger;

    public DocIndexer(
        IDocRepository repository,
        ITextEmbeddingGenerationService embeddingService,
        TextExtractorFactory extractorFactory,
        TextChunker chunker,
        IOptions<IndexingOptions> options,
        ILogger<DocIndexer> logger)
    {
        _repository = repository;
        _embeddingService = embeddingService;
        _extractorFactory = extractorFactory;
        _chunker = chunker;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<int> IndexAsync(string filePath, string protocol, CancellationToken ct = default)
    {
        var extracted = await _extractorFactory.ExtractAsync(filePath, ct);
        var chunks = _chunker.ChunkDocument(extracted, protocol);

        if (chunks.Count == 0) return 0;

        var batches = chunks.Chunk(_options.EmbeddingBatchSize);
        foreach (var batch in batches)
        {
            try
            {
                var texts = batch.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken: ct);
                for (int i = 0; i < batch.Length; i++)
                {
                    batch[i].Vector = embeddings[i].ToArray();
                }
                await _repository.UpsertAsync(batch, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "文档向量化/入库批次失败");
            }
        }

        return chunks.Count;
    }
}
