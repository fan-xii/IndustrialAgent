using System.Text.RegularExpressions;
using IndustrialAgent.Shared.Models;

namespace IndustrialAgent.Scaffolding;

public sealed class ScaffoldGenerator
{
    public ScaffoldPlanResponse GeneratePlan(string moduleName, string @namespace, string summary)
    {
        var response = new ScaffoldPlanResponse
        {
            ModuleName = moduleName,
            Summary = summary
        };

        var replacements = new Dictionary<string, string>
        {
            ["{{Namespace}}"] = @namespace,
            ["{{ModuleName}}"] = moduleName,
            ["{{ModuleNameCamel}}"] = char.ToLowerInvariant(moduleName[0]) + moduleName[1..]
        };

        response.Files.Add(new ScaffoldFile
        {
            Path = $"Interfaces/I{moduleName}Service.cs",
            Template = "ServiceInterface",
            Content = ApplyTemplate(Templates.TemplateProvider.ServiceInterfaceTemplate, replacements)
        });

        response.Files.Add(new ScaffoldFile
        {
            Path = $"Services/{moduleName}Service.cs",
            Template = "Service",
            Content = ApplyTemplate(Templates.TemplateProvider.ServiceImplTemplate, replacements)
        });

        response.Files.Add(new ScaffoldFile
        {
            Path = $"ViewModels/{moduleName}ViewModel.cs",
            Template = "ViewModel",
            Content = ApplyTemplate(Templates.TemplateProvider.ViewModelTemplate, replacements)
        });

        response.Files.Add(new ScaffoldFile
        {
            Path = $"Tests/{moduleName}ViewModelTests.cs",
            Template = "UnitTest",
            Content = ApplyTemplate(Templates.TemplateProvider.UnitTestTemplate, replacements)
        });

        return response;
    }

    public ScaffoldExecuteResponse Execute(ScaffoldPlanResponse plan, string targetDir)
    {
        var response = new ScaffoldExecuteResponse();

        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        foreach (var file in plan.Files)
        {
            var fullPath = Path.Combine(targetDir, plan.ModuleName, file.Path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, file.Content);
            response.CreatedFiles.Add(fullPath);
        }

        response.DiRegistrationSnippet = GenerateDiRegistration(plan.ModuleName);
        return response;
    }

    private static string ApplyTemplate(string template, Dictionary<string, string> replacements)
    {
        var result = template;
        foreach (var (key, value) in replacements)
        {
            result = result.Replace(key, value);
        }
        return result;
    }

    private static string GenerateDiRegistration(string moduleName)
    {
        var camelName = char.ToLowerInvariant(moduleName[0]) + moduleName[1..];
        return $"""
            // 在 App.xaml.cs 的 RegisterTypes 方法中添加：
            containerRegistry.RegisterSingleton<Interfaces.I{moduleName}Service, Services.{moduleName}Service>();
            containerRegistry.RegisterForNavigation<Views.{moduleName}View, ViewModels.{moduleName}ViewModel>();
            """;
    }

    public static string? ExtractModuleNameFromJson(string json)
    {
        var match = Regex.Match(json, @"""moduleName""\s*:\s*""([^""]+)""");
        return match.Success ? match.Groups[1].Value : null;
    }
}
