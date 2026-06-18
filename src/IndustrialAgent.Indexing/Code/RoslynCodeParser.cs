using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;

namespace IndustrialAgent.Indexing.Code;

public sealed class ParsedSolution
{
    public string Name { get; set; } = string.Empty;
    public List<ParsedProject> Projects { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public sealed class ParsedProject
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<Document> Documents { get; set; } = new();
}

public sealed class RoslynCodeParser
{
    private readonly ILogger<RoslynCodeParser> _logger;

    public RoslynCodeParser(ILogger<RoslynCodeParser> logger)
    {
        _logger = logger;
    }

    public async Task<ParsedSolution> ParseAsync(string solutionOrProjectPath, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(solutionOrProjectPath).ToLowerInvariant();
        return ext switch
        {
            ".sln" => await ParseSolutionAsync(solutionOrProjectPath, ct),
            ".csproj" => await ParseProjectAsync(solutionOrProjectPath, ct),
            _ => throw new NotSupportedException($"仅支持 .sln 和 .csproj 文件，当前：{ext}")
        };
    }

    private async Task<ParsedSolution> ParseSolutionAsync(string slnPath, CancellationToken ct)
    {
        var result = new ParsedSolution { Name = Path.GetFileNameWithoutExtension(slnPath) };

        MSBuildWorkspace workspace = CreateWorkspace();

        Solution solution;
        try
        {
            solution = await workspace.OpenSolutionAsync(slnPath, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开解决方案失败：{Path}", slnPath);
            result.Errors.Add($"打开解决方案失败：{ex.Message}");
            return result;
        }

        foreach (var project in solution.Projects)
        {
            var parsedProject = new ParsedProject
            {
                Name = project.Name,
                FilePath = project.FilePath ?? string.Empty
            };

            foreach (var doc in project.Documents)
            {
                if (doc.SourceCodeKind != SourceCodeKind.Regular) continue;
                if (!doc.SupportsSyntaxTree) continue;
                parsedProject.Documents.Add(doc);
            }

            result.Projects.Add(parsedProject);
        }

        result.Errors.AddRange(workspace.Diagnostics.Select(d => $"{d.Kind}: {d.Message}"));
        return result;
    }

    private async Task<ParsedSolution> ParseProjectAsync(string csprojPath, CancellationToken ct)
    {
        var result = new ParsedSolution { Name = Path.GetFileNameWithoutExtension(csprojPath) };

        MSBuildWorkspace workspace = CreateWorkspace();

        Project project;
        try
        {
            project = await workspace.OpenProjectAsync(csprojPath, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开项目失败：{Path}", csprojPath);
            result.Errors.Add($"打开项目失败：{ex.Message}");
            return result;
        }

        var parsedProject = new ParsedProject
        {
            Name = project.Name,
            FilePath = project.FilePath ?? string.Empty
        };

        foreach (var doc in project.Documents)
        {
            if (doc.SourceCodeKind != SourceCodeKind.Regular) continue;
            if (!doc.SupportsSyntaxTree) continue;
            parsedProject.Documents.Add(doc);
        }

        result.Projects.Add(parsedProject);
        result.Errors.AddRange(workspace.Diagnostics.Select(d => $"{d.Kind}: {d.Message}"));
        return result;
    }

    private MSBuildWorkspace CreateWorkspace()
    {
        var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (sender, e) =>
        {
            _logger.LogWarning("Roslyn 工作区错误 {Kind}: {Message}", e.Diagnostic.Kind, e.Diagnostic.Message);
        };
        return workspace;
    }
}
