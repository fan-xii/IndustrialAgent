using IndustrialAgent.Shared.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI;
using System.ClientModel;

namespace IndustrialAgent.Core;

public static class KernelBuilderExtensions
{
    public static IServiceCollection AddIndustrialAgentKernel(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var zhipuOptions = sp.GetRequiredService<IOptions<ZhipuAIOptions>>().Value;

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(zhipuOptions.Endpoint),
            NetworkTimeout = TimeSpan.FromMinutes(zhipuOptions.NetworkTimeoutMinutes)
        };

        var openAIClient = new OpenAIClient(new ApiKeyCredential(zhipuOptions.ApiKey), clientOptions);

        services.AddSingleton(openAIClient);

        services.AddKernel()
            .AddOpenAIChatCompletion(zhipuOptions.ModelId, openAIClient)
            .AddOpenAITextEmbeddingGeneration(zhipuOptions.EmbeddingModelId, openAIClient);

        services.AddIndustrialAgentPlugins();

        return services;
    }

    private static IServiceCollection AddIndustrialAgentPlugins(this IServiceCollection services)
    {
        services.AddSingleton<Plugins.CodeSearchPlugin>();
        services.AddSingleton<Plugins.DocSearchPlugin>();
        services.AddSingleton<Plugins.CodeGenPlugin>();
        services.AddSingleton<Plugins.LogDiagnosePlugin>();
        services.AddSingleton<Plugins.ScaffoldPlugin>();

        services.AddTransient<Kernel>(sp =>
        {
            var kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                    sp.GetRequiredService<IOptions<ZhipuAIOptions>>().Value.ModelId,
                    sp.GetRequiredService<OpenAIClient>())
                .AddOpenAITextEmbeddingGeneration(
                    sp.GetRequiredService<IOptions<ZhipuAIOptions>>().Value.EmbeddingModelId,
                    sp.GetRequiredService<OpenAIClient>())
                .Build();

            kernel.Plugins.AddFromObject(sp.GetRequiredService<Plugins.CodeSearchPlugin>(), "CodeSearch");
            kernel.Plugins.AddFromObject(sp.GetRequiredService<Plugins.DocSearchPlugin>(), "DocSearch");
            kernel.Plugins.AddFromObject(sp.GetRequiredService<Plugins.CodeGenPlugin>(), "CodeGen");
            kernel.Plugins.AddFromObject(sp.GetRequiredService<Plugins.LogDiagnosePlugin>(), "LogDiagnose");
            kernel.Plugins.AddFromObject(sp.GetRequiredService<Plugins.ScaffoldPlugin>(), "Scaffold");

            return kernel;
        });

        return services;
    }
}
