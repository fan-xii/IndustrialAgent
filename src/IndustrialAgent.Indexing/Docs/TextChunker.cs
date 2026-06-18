using System.Text;
using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Shared.Models;
using Microsoft.Extensions.Options;

namespace IndustrialAgent.Indexing.Docs;

public sealed class TextChunker
{
    private readonly IndexingOptions _options;

    public TextChunker(IOptions<IndexingOptions> options)
    {
        _options = options.Value;
    }

    public List<DocChunk> ChunkDocument(ExtractedDocument document, string protocol)
    {
        var chunks = new List<DocChunk>();
        var chunkIndex = 0;

        foreach (var page in document.Pages)
        {
            var pageChunks = SplitText(page.Text, _options.MaxDocChunkTokens, _options.ChunkOverlapTokens);
            foreach (var text in pageChunks)
            {
                if (string.IsNullOrWhiteSpace(text)) continue;
                chunks.Add(new DocChunk
                {
                    DocName = document.FileName,
                    Protocol = protocol,
                    Page = page.PageNumber,
                    ChunkIndex = chunkIndex++,
                    Section = ExtractSection(text),
                    Content = text.Trim()
                });
            }
        }

        return chunks;
    }

    private static List<string> SplitText(string text, int maxTokens, int overlap)
    {
        var result = new List<string>();
        if (string.IsNullOrEmpty(text)) return result;

        var paragraphs = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var current = new StringBuilder();
        var currentTokens = 0;

        foreach (var para in paragraphs)
        {
            var paraTokens = EstimateTokens(para);
            if (currentTokens + paraTokens > maxTokens && current.Length > 0)
            {
                result.Add(current.ToString());
                var overlapText = GetOverlapText(current.ToString(), overlap);
                current.Clear();
                current.Append(overlapText);
                currentTokens = EstimateTokens(overlapText);
            }

            current.AppendLine(para);
            currentTokens += paraTokens;
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    private static string GetOverlapText(string text, int overlapTokens)
    {
        var words = text.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var overlapWords = words.TakeLast(overlapTokens / 2);
        return string.Join(' ', overlapWords);
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return (int)Math.Ceiling(text.Length / 3.5);
    }

    private static string ExtractSection(string text)
    {
        var firstLine = text.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? string.Empty;
        if (firstLine.Length > 80) firstLine = firstLine[..80];
        return firstLine.Trim();
    }
}
