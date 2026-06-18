using IndustrialAgent.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace IndustrialAgent.Indexing.Code;

public sealed class CodeSlicer
{
    public async Task<List<CodeChunk>> SliceAsync(Document document, string projectName, CancellationToken ct = default)
    {
        var chunks = new List<CodeChunk>();

        SyntaxTree? tree = await document.GetSyntaxTreeAsync(ct);
        if (tree is null) return chunks;

        SemanticModel? model = await document.GetSemanticModelAsync(ct);
        SyntaxNode root = await tree.GetRootAsync(ct);
        var filePath = document.FilePath ?? document.Name;
        var fileHash = ComputeHash(filePath);

        var walker = new SymbolCollector(model, filePath, projectName, fileHash);
        walker.Visit(root);
        chunks.AddRange(walker.Chunks);

        return chunks;
    }

    private static string ComputeHash(string filePath)
    {
        if (!File.Exists(filePath)) return string.Empty;
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}

internal sealed class SymbolCollector : CSharpSyntaxWalker
{
    private readonly SemanticModel? _model;
    private readonly string _filePath;
    private readonly string _projectName;
    private readonly string _fileHash;

    public List<CodeChunk> Chunks { get; } = new();

    public SymbolCollector(SemanticModel? model, string filePath, string projectName, string fileHash)
    {
        _model = model;
        _filePath = filePath;
        _projectName = projectName;
        _fileHash = fileHash;
    }

    public override void VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        AddChunk(node, node.Identifier.Text, "class", node.GetText().ToString());
        base.VisitClassDeclaration(node);
    }

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
    {
        AddChunk(node, node.Identifier.Text, "interface", node.GetText().ToString());
        base.VisitInterfaceDeclaration(node);
    }

    public override void VisitStructDeclaration(StructDeclarationSyntax node)
    {
        AddChunk(node, node.Identifier.Text, "struct", node.GetText().ToString());
        base.VisitStructDeclaration(node);
    }

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        AddChunk(node, node.Identifier.Text, "record", node.GetText().ToString());
        base.VisitRecordDeclaration(node);
    }

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        var name = node.Identifier.Text;
        AddChunk(node, name, "method", node.ToString());
        base.VisitMethodDeclaration(node);
    }

    public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        AddChunk(node, node.Identifier.Text, "property", node.ToString());
        base.VisitPropertyDeclaration(node);
    }

    private void AddChunk(SyntaxNode node, string symbolName, string kind, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        if (content.Length < 20) return;

        var tree = node.SyntaxTree;
        var span = tree.GetLineSpan(node.Span);
        var startLine = span.StartLinePosition.Line + 1;
        var endLine = span.EndLinePosition.Line + 1;

        string qualifiedName = symbolName;
        string ns = string.Empty;
        string containingType = string.Empty;

        if (_model is not null)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            if (symbol is not null)
            {
                qualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                ns = symbol.ContainingNamespace?.ToDisplayString() ?? string.Empty;
                if (symbol.ContainingType is not null)
                {
                    containingType = symbol.ContainingType.ToDisplayString();
                }
            }
        }

        if (content.Length > 4000)
        {
            content = content[..4000] + "\n// ... [代码片段过长，已截断]";
        }

        Chunks.Add(new CodeChunk
        {
            QualifiedName = qualifiedName,
            SymbolName = symbolName,
            Kind = kind,
            FilePath = _filePath,
            StartLine = startLine,
            EndLine = endLine,
            Namespace = ns,
            ContainingType = containingType,
            Content = content,
            ProjectName = _projectName,
            FileHash = _fileHash
        });
    }
}
