namespace IndustrialAgent.Shared.Configuration;

public sealed class ZhipuAIOptions
{
    public const string SectionName = "ZhipuAI";

    public string ApiKey { get; set; } = string.Empty;
    public string ModelId { get; set; } = "glm-5.2";
    public string Endpoint { get; set; } = "https://open.bigmodel.cn/api/paas/v4/";
    public string EmbeddingModelId { get; set; } = "embedding-3";
    public int NetworkTimeoutMinutes { get; set; } = 10;
    public int MaxOutputTokens { get; set; } = 8192;
    public double Temperature { get; set; } = 0.3;
}

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
    public string CodeCollection { get; set; } = "code_chunks";
    public string DocCollection { get; set; } = "doc_chunks";
    public int VectorDimension { get; set; } = 1024;
}

public sealed class IndexingOptions
{
    public const string SectionName = "Indexing";

    public int MaxCodeChunkTokens { get; set; } = 800;
    public int MaxDocChunkTokens { get; set; } = 1000;
    public int ChunkOverlapTokens { get; set; } = 150;
    public int EmbeddingBatchSize { get; set; } = 16;
    public int QdrantUpsertBatchSize { get; set; } = 100;
}

public sealed class WorkspaceOptions
{
    public const string SectionName = "Workspace";

    public string RootPath { get; set; } = string.Empty;
    public string AllowedWriteRoot { get; set; } = string.Empty;
    public string[] AllowedUploadExtensions { get; set; } = { ".pdf", ".docx", ".log", ".txt" };
}
