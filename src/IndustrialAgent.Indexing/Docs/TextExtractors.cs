using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace IndustrialAgent.Indexing.Docs;

public interface ITextExtractor
{
    bool Supports(string filePath);
    Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken ct = default);
}

public sealed class ExtractedDocument
{
    public string FileName { get; set; } = string.Empty;
    public List<ExtractedPage> Pages { get; set; } = new();
}

public sealed class ExtractedPage
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
}

public sealed class PdfTextExtractor : ITextExtractor
{
    public bool Supports(string filePath) =>
        Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken ct = default)
    {
        var doc = new ExtractedDocument { FileName = Path.GetFileName(filePath) };

        using var pdf = PdfDocument.Open(filePath, new ParsingOptions { UseLenientParsing = true });
        foreach (Page page in pdf.GetPages())
        {
            var words = page.GetWords();
            var sb = new StringBuilder();
            foreach (var word in words)
            {
                sb.Append(word.Text).Append(' ');
            }
            doc.Pages.Add(new ExtractedPage { PageNumber = page.Number, Text = sb.ToString().Trim() });
        }

        return Task.FromResult(doc);
    }
}

public sealed class WordTextExtractor : ITextExtractor
{
    public bool Supports(string filePath) =>
        Path.GetExtension(filePath).Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken ct = default)
    {
        var doc = new ExtractedDocument { FileName = Path.GetFileName(filePath) };

        using var word = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(filePath, isEditable: false);
        var body = word.MainDocumentPart?.Document.Body
            ?? throw new InvalidOperationException("Word 文档无正文内容。");

        var sb = new StringBuilder();
        foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
        {
            var text = string.Concat(para.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text));
            sb.AppendLine(text);
        }

        foreach (var table in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>())
        {
            foreach (var row in table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>())
            {
                var cells = row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
                    .Select(c => string.Concat(c.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)));
                sb.AppendLine(string.Join(" | ", cells));
            }
        }

        doc.Pages.Add(new ExtractedPage { PageNumber = 1, Text = sb.ToString().Trim() });
        return Task.FromResult(doc);
    }
}

public sealed class PlainTextExtractor : ITextExtractor
{
    public bool Supports(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext is ".txt" or ".log";
    }

    public async Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken ct = default)
    {
        var text = await File.ReadAllTextAsync(filePath, ct);
        return new ExtractedDocument
        {
            FileName = Path.GetFileName(filePath),
            Pages = { new ExtractedPage { PageNumber = 1, Text = text } }
        };
    }
}

public sealed class TextExtractorFactory
{
    private readonly List<ITextExtractor> _extractors;

    public TextExtractorFactory()
    {
        _extractors = new List<ITextExtractor>
        {
            new PdfTextExtractor(),
            new WordTextExtractor(),
            new PlainTextExtractor()
        };
    }

    public ITextExtractor GetExtractor(string filePath)
    {
        var extractor = _extractors.FirstOrDefault(e => e.Supports(filePath))
            ?? throw new NotSupportedException($"不支持的文件类型：{Path.GetExtension(filePath)}");
        return extractor;
    }

    public async Task<ExtractedDocument> ExtractAsync(string filePath, CancellationToken ct = default)
    {
        var extractor = GetExtractor(filePath);
        return await extractor.ExtractAsync(filePath, ct);
    }
}
