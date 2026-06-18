namespace IndustrialAgent.Shared.Models;

public sealed class CodeQaRequest
{
    public string Question { get; set; } = string.Empty;
    public string? FilePathFilter { get; set; }
    public string? KindFilter { get; set; }
    public bool Stream { get; set; }
}

public sealed class CodeQaResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SearchResult> References { get; set; } = new();
}

public sealed class DocUploadRequest
{
    public string Protocol { get; set; } = string.Empty;
    public string DocName { get; set; } = string.Empty;
}

public sealed class DocRagRequest
{
    public string Question { get; set; } = string.Empty;
    public string? Protocol { get; set; }
}

public sealed class DocRagResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<SearchResult> References { get; set; } = new();
}

public sealed class CodeGenRequest
{
    public string Type { get; set; } = string.Empty;
    public string Requirement { get; set; } = string.Empty;
    public string ModuleName { get; set; } = string.Empty;
    public bool WriteToDisk { get; set; }
}

public sealed class CodeGenResponse
{
    public string Code { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string Language { get; set; } = "csharp";
    public bool WrittenToDisk { get; set; }
}

public sealed class LogDiagnoseRequest
{
    public string LogContent { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}

public sealed class LogDiagnoseResponse
{
    public string RootCause { get; set; } = string.Empty;
    public string CodeLocation { get; set; } = string.Empty;
    public string FixSuggestion { get; set; } = string.Empty;
    public string CodeExample { get; set; } = string.Empty;
    public List<SearchResult> References { get; set; } = new();
}

public sealed class ScaffoldPlanRequest
{
    public string Requirement { get; set; } = string.Empty;
}

public sealed class ScaffoldFile
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Template { get; set; } = string.Empty;
}

public sealed class ScaffoldPlanResponse
{
    public string ModuleName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<ScaffoldFile> Files { get; set; } = new();
}

public sealed class ScaffoldExecuteRequest
{
    public string TargetDir { get; set; } = string.Empty;
    public ScaffoldPlanResponse Plan { get; set; } = new();
}

public sealed class ScaffoldExecuteResponse
{
    public List<string> CreatedFiles { get; set; } = new();
    public string DiRegistrationSnippet { get; set; } = string.Empty;
}

public sealed class IndexProjectRequest
{
    public string SolutionOrProjectPath { get; set; } = string.Empty;
    public bool ForceReindex { get; set; }
}

public sealed class IndexProjectResponse
{
    public int TotalDocuments { get; set; }
    public int TotalChunks { get; set; }
    public int FailedDocuments { get; set; }
    public List<string> Errors { get; set; } = new();
}
