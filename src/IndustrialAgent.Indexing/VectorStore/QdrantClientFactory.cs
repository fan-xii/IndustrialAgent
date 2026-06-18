using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Shared.Models;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using FieldType = Qdrant.Client.Grpc.PayloadSchemaType;

namespace IndustrialAgent.Indexing.VectorStore;

public sealed class QdrantClientFactory : IDisposable
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public QdrantClientFactory(IOptions<QdrantOptions> options)
    {
        _options = options.Value;
        _client = new QdrantClient(_options.Host, _options.Port);
    }

    public QdrantClient GetClient() => _client;

    public async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;
            await EnsureCollectionAsync(_options.CodeCollection, ct);
            await EnsureCollectionAsync(_options.DocCollection, ct);
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task EnsureCollectionAsync(string collectionName, CancellationToken ct)
    {
        var collections = await _client.ListCollectionsAsync(ct);
        if (collections.Contains(collectionName)) return;

        await _client.CreateCollectionAsync(
            collectionName: collectionName,
            vectorsConfig: new VectorParams
            {
                Size = (ulong)_options.VectorDimension,
                Distance = Distance.Cosine
            },
            cancellationToken: ct);

        await CreatePayloadIndexAsync(collectionName, "filePath", FieldType.Keyword, ct);
        await CreatePayloadIndexAsync(collectionName, "symbolName", FieldType.Keyword, ct);
        await CreatePayloadIndexAsync(collectionName, "kind", FieldType.Keyword, ct);
        await CreatePayloadIndexAsync(collectionName, "docName", FieldType.Keyword, ct);
        await CreatePayloadIndexAsync(collectionName, "protocol", FieldType.Keyword, ct);
        await CreatePayloadIndexAsync(collectionName, "content", FieldType.Text, ct);
    }

    private async Task CreatePayloadIndexAsync(string collectionName, string fieldName, PayloadSchemaType schemaType, CancellationToken ct)
    {
        try
        {
            await _client.CreatePayloadIndexAsync(
                collectionName: collectionName,
                fieldName: fieldName,
                schemaType: schemaType,
                cancellationToken: ct);
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _initLock.Dispose();
    }
}
