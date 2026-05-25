using EmbeddingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var ollamaUrl = builder.Configuration.GetValue<string>("Ollama:Url") ?? "http://ollama:11434";
var clipUrl = builder.Configuration.GetValue<string>("Clip:Url") ?? "http://clip:51000";

builder.Services.AddSingleton<ITextEmbedder>(_ => new OllamaTextEmbedder(ollamaUrl));
builder.Services.AddSingleton<IImageEmbedder>(_ => new ClipImageEmbedder(clipUrl));

var app = builder.Build();

app.MapControllers();

app.Run();
