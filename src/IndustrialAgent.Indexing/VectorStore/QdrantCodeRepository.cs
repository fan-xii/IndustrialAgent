using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Shared.Models;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace IndustrialAgent.Indexing.VectorStore;

public interface ICodeRepository
{
    Task UpsertAsync(IEnumerable<CodeChunk> chunks, CancellationToken ct = default);
    Task<List<SearchResult>> SearchAsync(float[] queryVector, int topK, string? filePathFilter = null, string? kindFilter = null, CancellationToken ct = default);
    Task DeleteByFileAsync(string filePath, CancellationToken ct = default);
    Task<long> CountAsync(CancellationToken ct = default);
}

public sealed class QdrantCodeRepository : ICodeRepository
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly QdrantClientFactory _factory;

    public QdrantCodeRepository(QdrantClientFactory factory, IOptions<QdrantOptions> options)
    {
        _factory = factory;
        _options = options.Value;
        _client = factory.GetClient();
    }

    public async Task UpsertAsync(IEnumerable<CodeChunk> chunks, CancellationToken ct = default)
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
                    ["qualifiedName"] = chunk.QualifiedName,
                    ["symbolName"] = chunk.SymbolName,
                    ["kind"] = chunk.Kind,
                    ["filePath"] = chunk.FilePath,
                    ["startLine"] = (long)chunk.StartLine,
                    ["endLine"] = (long)chunk.EndLine,
                    ["namespace"] = chunk.Namespace,
                    ["containingType"] = chunk.ContainingType,
                    ["content"] = chunk.Content,
                    ["projectName"] = chunk.ProjectName,
                    ["fileHash"] = chunk.FileHash,
                }
            });

            if (batch.Count >= 100)
            {
                await _client.UpsertAsync(_options.CodeCollection, batch, wait: true, cancellationToken: ct);
                batch.Clear();
            }
        }

        if (batch.Count > 0)
        {
            await _client.UpsertAsync(_options.CodeCollection, batch, wait: true, cancellationToken: ct);
        }
    }

    public async Task<List<SearchResult>> SearchAsync(float[] queryVector, int topK, string? filePathFilter = null, string? kindFilter = null, CancellationToken ct = default)
    {
        await _factory.EnsureInitializedAsync(ct);

        Filter? filter = null;
        var conditions = new List<Condition>();
        if (!string.IsNullOrEmpty(filePathFilter))
        {
            conditions.Add(new Condition
            {
                Field = new FieldCondition { Key = "filePath", Match = new Match { Keyword = filePathFilter } }
            });
        }
        if (!string.IsNullOrEmpty(kindFilter))
        {
            conditions.Add(new Condition
            {
                Field = new FieldCondition { Key = "kind", Match = new Match { Keyword = kindFilter } }
            });
        }
        if (conditions.Count > 0)
        {
            filter = new Filter { Must = { conditions } };
        }

        var hits = await _client.SearchAsync(
            collectionName: _options.CodeCollection,
            vector: queryVector,
            filter: filter,
            limit: (ulong)topK,
            payloadSelector: true,
            cancellationToken: ct);

        return hits.Select(ToSearchResult).ToList();
    }

    public async Task DeleteByFileAsync(string filePath, CancellationToken ct = default)
    {
        await _factory.EnsureInitializedAsync(ct);
        await _client.DeleteAsync(_options.CodeCollection,
            filter: new Filter { Must = { new Condition { Field = new FieldCondition {
                Key = "filePath", Match = new Match { Keyword = filePath } } } } },
            wait: true, cancellationToken: ct);
    }

    public async Task<long> CountAsync(CancellationToken ct = default)
    {
        await _factory.EnsureInitializedAsync(ct);
        var info = await _client.GetCollectionInfoAsync(_options.CodeCollection, ct);
        return (long)info.PointsCount;
    }

    private static SearchResult ToSearchResult(ScoredPoint point)
    {
        var result = new SearchResult
        {
            Id = point.Id?.Uuid ?? point.Id?.Num.ToString() ?? string.Empty,
            Score = point.Score,
            Content = point.Payload.TryGetValue("content", out var c) ? c.StringValue : string.Empty
        };
        foreach (var (key, value) in point.Payload)
        {
            result.Metadata[key] = value.StringValue ?? value.IntegerValue.ToString() ?? value.ToString();
        }
        return result;
    }
}
