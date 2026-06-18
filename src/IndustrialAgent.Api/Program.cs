using IndustrialAgent.Core;
using IndustrialAgent.Core.Plugins;
using IndustrialAgent.Indexing.Code;
using IndustrialAgent.Indexing.Docs;
using IndustrialAgent.Indexing.VectorStore;
using IndustrialAgent.Shared.Configuration;
using IndustrialAgent.Scaffolding;
using System.ClientModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ZhipuAIOptions>(builder.Configuration.GetSection(ZhipuAIOptions.SectionName));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection(QdrantOptions.SectionName));
builder.Services.Configure<IndexingOptions>(builder.Configuration.GetSection(IndexingOptions.SectionName));
builder.Services.Configure<WorkspaceOptions>(builder.Configuration.GetSection(WorkspaceOptions.SectionName));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<QdrantClientFactory>();
builder.Services.AddSingleton<ICodeRepository, QdrantCodeRepository>();
builder.Services.AddSingleton<IDocRepository, QdrantDocRepository>();

builder.Services.AddSingleton<TextExtractorFactory>();
builder.Services.AddSingleton<TextChunker>();

builder.Services.AddSingleton<RoslynCodeParser>();
builder.Services.AddSingleton<CodeIndexer>();
builder.Services.AddSingleton<DocIndexer>();

builder.Services.AddSingleton<ScaffoldGenerator>();

RegisterKernelAndPlugins(builder.Services, builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();

static void RegisterKernelAndPlugins(IServiceCollection services, IConfiguration configuration)
{
    var zhipu = configuration.GetSection(ZhipuAIOptions.SectionName).Get<ZhipuAIOptions>()!;

    var clientOptions = new OpenAIClientOptions
    {
        Endpoint = new Uri(zhipu.Endpoint),
        NetworkTimeout = TimeSpan.FromMinutes(zhipu.NetworkTimeoutMinutes)
    };
    var openAIClient = new OpenAIClient(new ApiKeyCredential(zhipu.ApiKey), clientOptions);

    services.AddSingleton(openAIClient);
    services.AddSingleton<ITextEmbeddingGenerationService>(sp =>
    {
        var kernel = sp.GetRequiredService<Kernel>();
        return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    });

    services.AddKernel()
        .AddOpenAIChatCompletion(zhipu.ModelId, openAIClient)
        .AddOpenAITextEmbeddingGeneration(zhipu.EmbeddingModelId, openAIClient);

    services.AddSingleton<CodeSearchPlugin>();
    services.AddSingleton<DocSearchPlugin>();
    services.AddSingleton<CodeGenPlugin>();
    services.AddSingleton<LogDiagnosePlugin>();
    services.AddSingleton<ScaffoldPlugin>();

    services.AddTransient<Kernel>(sp =>
    {
        var builder2 = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(zhipu.ModelId, sp.GetRequiredService<OpenAIClient>())
            .AddOpenAITextEmbeddingGeneration(zhipu.EmbeddingModelId, sp.GetRequiredService<OpenAIClient>());

        var kernel = builder2.Build();

        kernel.Plugins.AddFromObject(sp.GetRequiredService<CodeSearchPlugin>(), "CodeSearch");
        kernel.Plugins.AddFromObject(sp.GetRequiredService<DocSearchPlugin>(), "DocSearch");
        kernel.Plugins.AddFromObject(sp.GetRequiredService<CodeGenPlugin>(), "CodeGen");
        kernel.Plugins.AddFromObject(sp.GetRequiredService<LogDiagnosePlugin>(), "LogDiagnose");
        kernel.Plugins.AddFromObject(sp.GetRequiredService<ScaffoldPlugin>(), "Scaffold");

        return kernel;
    });
}
