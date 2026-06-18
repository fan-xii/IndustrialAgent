using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Shared.Models;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace IndustrialAgent.Indexing.VectorStore;

public interface IDocRepository
{
    Task UpsertAsync(IEnumerable<DocChunk> chunks, CancellationToken ct = default);
    Task<List<SearchResult>> SearchAsync(float[] queryVector, int topK, string? protocolFilter = null, CancellationToken ct = default);
    Task DeleteByDocAsync(string docName, CancellationToken ct = default);
}

public sealed class QdrantDocRepository : IDocRepository
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly QdrantClientFactory _factory;

    public QdrantDocRepository(QdrantClientFactory factory, IOptions<QdrantOptions> options)
    {
        _factory = factory;
        _options = options.Value;
        _client = factory.GetClient();
    }

    public async Task UpsertAsync(IEnumerable<DocChunk> chunks, CancellationToken ct = default)
    {
        await _factory.EnsureInitializedAsync(ct);
        var batch = new List<PointStruct>();

        foreach (var chunk in chunks)
        {
            if (chunk.Vector is null || chunk.Vector.Length == 0) continue;

            batch.Add(new PointStruct
            {
                Id = Guid.Parse(chunk.Id),
                Vectors = chunk.Vector,
                Payload =
                {
                    ["docName"] = chunk.DocName,
                    ["protocol"] = chunk.Protocol,
                    ["page"] = (long)chunk.Page,
                    ["chunkIndex"] = (long)chunk.ChunkIndex,
                    ["section"] = chunk.Section,
                    ["content"] = chunk.Content,
                }
            });

            if (batch.Count >= 100)
            {
                await _client.UpsertAsync(_options.DocCollection, batch, wait: true, cancellationToken: ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await _client.UpsertAsync(_options.DocCollection, batch, wait: true, cancellationToken: ct);
        }
    }

    public async Task<List<SearchResult>> SearchAsync(float[] queryVector, int topK, string? protocolFilter = null, CancellationToken ct = default)
    {
        await _factory.EnsureInitializedAsync(ct);

        Filter? filter = null;
        if (!string.IsNullOrEmpty(protocolFilter))
        {
            filter = new Filter { Must = { new Condition { Field = new FieldCondition {
                Key = "protocol", Match = new Match { Keyword = protocolFilter } } } } };
        }

        var hits = await _client.SearchAsync(
            collectionName: _options.DocCollection,
            vector: queryVector,
            filter: filter,
            limit: (ulong)topK,
            payloadSelector: true,
            cancellationToken: ct);

        return hits.Select(p => new SearchResult
        {
            Id = p.Id?.Uuid ?? string.Empty,
            Score = p.Score,
            Content = p.Payload.TryGetValue("content", out var c) ? c.StringValue : string.Empty,
            Metadata = p.Payload.ToDictionary(k => k.Key, v => v.Value.StringValue ?? v.Value.IntegerValue.ToString() ?? v.Value.ToString())
        }).ToList();
    }

    public async Task DeleteByDocAsync(string docName, CancellationToken ct = default)
    {
        await _factory.EnsureInitializedAsync(ct);
        await _client.DeleteAsync(_options.DocCollection,
            filter: new Filter { Must = { new Condition { Field = new FieldCondition {
                Key = "docName", Match = new Match { Keyword = docName } } } } },
            wait: true, cancellationToken: ct);
    }
}
