namespace IndustrialAgent.Shared.Models;

public sealed class CodeChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string QualifiedName { get; set; } = string.Empty;
    public string SymbolName { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public string Namespace { get; set; } = string.Empty;
    public string ContainingType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;

    public float[]? Vector { get; set; }
}

public sealed class DocChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DocName { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public int Page { get; set; }
    public int ChunkIndex { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public float[]? Vector { get; set; }
}

public sealed class SearchResult
{
    public string Id { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
}
